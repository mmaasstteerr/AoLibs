﻿using System.Collections.Generic;
using Android.Views;
using GalaSoft.MvvmLight.Helpers;

namespace AoLibs.Adapters.Android.Recycler
{
    /// <summary>
    /// Base class for ViewHolder which hold separate bindings for each items.
    /// </summary>
    /// <typeparam name="T">Type of ViewModel associated with iven holder.</typeparam>
    public abstract class BindingViewHolderBase<T> : BindingViewHolderNonGenericBase
    {
        private T _viewModel;

        protected List<Binding> Bindings { get; } = new List<Binding>();

        /// <summary>
        /// Initializes a new instance of the <see cref="BindingViewHolderBase{T}"/> class.
        /// </summary>
        /// <param name="view">The view the holder is associated with.</param>
        public BindingViewHolderBase(View view) 
            : base(view)
        {
        }

        /// <summary>
        /// Gets or sets associated ViewModel.
        /// Setting new instance will cause all bindings to rewired towards new one.
        /// </summary>
        public T ViewModel
        {
            get => _viewModel;            
            set
            {
                if (_viewModel != null)
                {
                    PreviousViewModel = _viewModel;
                }

                _viewModel = value;
                foreach (var binding in Bindings)               
                    binding.Detach();
                Bindings.Clear();         
                SetBindings();
            }
        }

        /// <summary>
        /// Gets or sets the ViewModel that was assigned before current one.
        /// Will be null if there was previous one.
        /// </summary>
        public T PreviousViewModel { get; set; }

        /// <summary>
        /// Detaches all registered bindings.
        /// </summary>
        public override void DetachBindings()
        {
            foreach (var binding in Bindings)           
                binding.Detach();           
        }

        /// <summary>
        /// Method called whenever need araises for the bindings to be defined.
        /// Bindings should be registerred in <see cref="Bindings"/> collection.
        /// </summary>
        protected abstract void SetBindings();
    }
}