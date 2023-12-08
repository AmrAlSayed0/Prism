﻿using System.ComponentModel;

namespace Prism.Mvvm;

/// <summary>
/// This class defines the attached property and related change handler that calls the <see cref="Prism.Mvvm.ViewModelLocationProvider"/>.
/// </summary>
public static class ViewModelLocator
{
    /// <summary>
    /// Instructs Prism whether or not to automatically create an instance of a ViewModel using a convention, and assign the associated View's <see cref="BindableObject.BindingContext"/> to that instance.
    /// </summary>
    public static readonly BindableProperty AutowireViewModelProperty =
        BindableProperty.CreateAttached("AutowireViewModel", typeof(ViewModelLocatorBehavior), typeof(ViewModelLocator), ViewModelLocatorBehavior.Automatic, propertyChanged: OnAutowireViewModelChanged);

    internal static readonly BindableProperty ViewModelProperty =
        BindableProperty.CreateAttached("ViewModelType",
            typeof(Type),
            typeof(ViewModelLocator),
            null,
            propertyChanged: OnViewModelPropertyChanged);

    internal static readonly BindableProperty NavigationNameProperty =
        BindableProperty.CreateAttached("NavigationName", typeof(string), typeof(ViewModelLocator), null);

    internal static string GetNavigationName(BindableObject bindable) =>
        (string)bindable.GetValue(NavigationNameProperty);

    internal static void SetNavigationName(BindableObject bindable, string name) =>
        bindable.SetValue(NavigationNameProperty, name);

    /// <summary>
    /// Gets the AutowireViewModel property value.
    /// </summary>
    /// <param name="bindable"></param>
    /// <returns></returns>
    public static ViewModelLocatorBehavior GetAutowireViewModel(BindableObject bindable)
    {
        return (ViewModelLocatorBehavior)bindable.GetValue(AutowireViewModelProperty);
    }

    /// <summary>
    /// Sets the AutowireViewModel property value.  If <c>true</c>, creates an instance of a ViewModel using a convention, and sets the associated View's <see cref="BindableObject.BindingContext"/> to that instance.
    /// </summary>
    /// <param name="bindable"></param>
    /// <param name="value"></param>
    public static void SetAutowireViewModel(BindableObject bindable, ViewModelLocatorBehavior value)
    {
        bindable.SetValue(AutowireViewModelProperty, value);
    }

    private static void OnViewModelPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (newValue == null || bindable.BindingContext != null)
            return;
        else if(newValue is Type)
            bindable.SetValue(AutowireViewModelProperty, ViewModelLocatorBehavior.Automatic);
    }

    private static void OnAutowireViewModelChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (newValue is not ViewModelLocatorBehavior behavior)
        {
            return;
        }
        else if (behavior == ViewModelLocatorBehavior.ForceLoaded)
        {
            AutowireInternal(bindable);
        }
        else if (behavior == ViewModelLocatorBehavior.WhenAvailable)
        {
            if (bindable is Page page)
            {
                MonitorPage(page, () => AutowireInternal(page));
            }
        }
    }

    private static void MonitorPage(Page page, Action autowireCallback)
    {
        var container = Navigation.Xaml.Navigation.GetContainerProvider(page);
        if (container is not null)
        {
            autowireCallback();
            return;
        }

        page.PropertyChanged += OnContainerProviderSet;
        void OnContainerProviderSet(object view, PropertyChangedEventArgs args)
        {
            if (args.PropertyName != Navigation.Xaml.Navigation.NavigationScopeProperty.PropertyName)
                return;

            page.PropertyChanged -= OnContainerProviderSet;
            autowireCallback();
        }
    }

    internal static void Autowire(object view)
    {
        if (view is Element element &&
            ((ViewModelLocatorBehavior)element.GetValue(AutowireViewModelProperty) == ViewModelLocatorBehavior.Disabled
            || (element.BindingContext is not null && element.BindingContext != element.Parent)))
            return;

        else if(view is TabbedPage tabbed)
        {
            foreach (var child in tabbed.Children)
                Autowire(child);
        }
        else if(view is NavigationPage navigationPage && navigationPage.RootPage is not null)
        {
            Autowire(navigationPage.RootPage);
        }

        AutowireInternal(view);
    }

    private static void AutowireInternal(object view)
    {
        ViewModelLocationProvider.AutoWireViewModelChanged(view, Bind);

        if (view is BindableObject bindable && bindable.BindingContext is null)
            bindable.BindingContext = new object();
    }

    /// <summary>
    /// Sets the <see cref="BindableObject.BindingContext"/> of a View
    /// </summary>
    /// <param name="view">The View to set the <see cref="BindableObject.BindingContext"/> on</param>
    /// <param name="viewModel">The object to use as the <see cref="BindableObject.BindingContext"/> for the View</param>
    private static void Bind(object view, object viewModel)
    {
        if (view is BindableObject element)
            element.BindingContext = viewModel;
    }
}
