﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Android.Views;
using AoLibs.Navigation.Android.Navigation.Attributes;
using AoLibs.Navigation.Core;
using AoLibs.Navigation.Core.Interfaces;
using AoLibs.Navigation.Core.PageProviders;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;

namespace AoLibs.Navigation.Android.Navigation
{
    /// <summary>
    /// Class that fulfills the purpose of executing actual navigation transactions.
    /// </summary>
    /// <typeparam name="TPageIdentifier">Page enum type.</typeparam>
    public class NavigationManager<TPageIdentifier> : NavigationManagerBase<NavigationFragmentBase, TPageIdentifier>
    {
        private readonly FragmentManager _fragmentManager;
        private readonly ViewGroup _rootFrame;
        private readonly Action<FragmentTransaction> _interceptTransaction;

        /// <summary>
        /// Gets or sets a value indicating whether failed navigation should throw an exception.
        /// </summary>
        public bool ThrowOnNavigationException { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationManager{TPageIdentifier}"/> class.
        /// </summary>
        /// <param name="fragmentManager">Fragment manager of main activity.</param>
        /// <param name="rootFrame">The view which will be used as the one being replaced with new Views</param>
        /// <param name="pageDefinitions">The dictionary defining pages.</param>
        /// <param name="viewModelResolver">Class used to resolve ViewModels for pages derived from <see cref="FragmentBase{TViewModel}"/></param>
        /// <param name="stackResolver">Class allowing to differentiate to which stack given indentigier belongs.</param>
        /// <param name="interceptTransaction">Delegate allowing to modify <see cref="FragmentTransaction"/> before commiting.</param>
        public NavigationManager(
            FragmentManager fragmentManager,
            ViewGroup rootFrame,
            Dictionary<TPageIdentifier, IPageProvider<NavigationFragmentBase>> pageDefinitions,
            IViewModelResolver viewModelResolver = null,
            IStackResolver<NavigationFragmentBase, TPageIdentifier> stackResolver = null,
            Action<FragmentTransaction> interceptTransaction = null) 
            : base(pageDefinitions, stackResolver)
        {
            _fragmentManager = fragmentManager;
            _rootFrame = rootFrame;
            _interceptTransaction = interceptTransaction;

            NavigationFragmentBase.ViewModelResolver = viewModelResolver;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationManager{TPageIdentifier}"/> class.
        /// To gather page definitions it searches for classes marked with <see cref="NavigationPageAttribute"/> from <see cref="Assembly.GetCallingAssembly"/>
        /// </summary>
        /// <param name="fragmentManager">Fragment manager of main activity.</param>
        /// <param name="rootFrame">The view which will be used as the one being replaced with new Views</param>
        /// <param name="viewModelResolver">Class used to resolve ViewModels for pages derived from <see cref="FragmentBase{TViewModel}"/></param>
        /// <param name="stackResolver">Class allowing to differentiate to which stack given indentigier belongs.</param>
        /// <param name="interceptTransaction">Delegate allowing to modify <see cref="FragmentTransaction"/> before commiting.</param>
        public NavigationManager(
            FragmentManager fragmentManager, 
            ViewGroup rootFrame,
            IViewModelResolver viewModelResolver = null,
            IStackResolver<NavigationFragmentBase, TPageIdentifier> stackResolver = null,
            Action<FragmentTransaction> interceptTransaction = null)
            : base(stackResolver)
        {
            _fragmentManager = fragmentManager;
            _rootFrame = rootFrame;
            _interceptTransaction = interceptTransaction;

            NavigationFragmentBase.ViewModelResolver = viewModelResolver;

            var types = Assembly.GetCallingAssembly().GetTypes();

            foreach (var type in types)
            {
                var attr = type.GetTypeInfo().GetCustomAttribute<NavigationPageAttribute>();

                if (attr != null)
                {
                    IPageProvider<NavigationFragmentBase> provider = null;

                    switch (attr.PageProviderType)
                    {
                        case NavigationPageAttribute.PageProvider.Cached:
                            provider = ObtainProviderFromType(typeof(CachedPageProvider<>));
                            break;
                        case NavigationPageAttribute.PageProvider.Oneshot:
                            provider = ObtainProviderFromType(typeof(OneshotPageProvider<>));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    PageDefinitions.Add((TPageIdentifier)(object)attr.Page, provider);
                }

                IPageProvider<NavigationFragmentBase> ObtainProviderFromType(Type providerType)
                {
                    return (IPageProvider<NavigationFragmentBase>)providerType.MakeGenericType(type)
                        .GetConstructor(new Type[] { })
                        .Invoke(null);
                }
            }

            foreach (var pageDefinition in PageDefinitions)
            {
                pageDefinition.Value.PageIdentifier = pageDefinition.Key;
            }
        }

        public override void CommitPageTransaction(NavigationFragmentBase page)
        {
            try
            {
                var transaction = _fragmentManager.BeginTransaction();
                _interceptTransaction?.Invoke(transaction);
                transaction.Replace(_rootFrame.Id, page)
                           .DisallowAddToBackStack();
                transaction.CommitNowAllowingStateLoss();
                                           
                _fragmentManager.ExecutePendingTransactions();
            }
            catch (Exception e)
            {
                Console.WriteLine($"There was an issue navigating to idndicated page, please ensure everyhting is set-up correctly. Exception: {e}");
                if (ThrowOnNavigationException)
                    throw e;
            }
        }
    }
}