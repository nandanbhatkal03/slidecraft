using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using slidecraft.Models;

namespace slidecraft.ViewModels;

/// <summary>
/// ViewModel wrapper around a <see cref="SlideModel"/>. Exposes an
/// observable collection of element ViewModels and the slide's transition.
/// </summary>
public partial class SlideViewModel : ObservableObject
{
    /// <summary>The underlying slide model.</summary>
    public SlideModel Model { get; }

    /// <summary>Observable collection of element ViewModels on this slide.</summary>
    public ObservableCollection<ElementViewModel> Elements { get; } = new();

    /// <summary>1-based slide number for display in the thumbnail panel.</summary>
    [ObservableProperty] private int _slideNumber;

    /// <summary>The transition type for this slide.</summary>
    [ObservableProperty] private TransitionType _transition;

    partial void OnTransitionChanged(TransitionType value) => Model.Transition = value;

    /// <summary>Creates a new <see cref="SlideViewModel"/> wrapping the given model.</summary>
    public SlideViewModel(SlideModel model, int number)
    {
        Model = model;
        _slideNumber = number;
        _transition = model.Transition;

        foreach (var el in model.Elements)
            Elements.Add(new ElementViewModel(el));
    }

    /// <summary>Add an element to this slide (model + VM).</summary>
    public ElementViewModel AddElement(SlideElement element)
    {
        Model.Elements.Add(element);
        var vm = new ElementViewModel(element);
        Elements.Add(vm);
        return vm;
    }

    /// <summary>Remove an element from this slide (model + VM).</summary>
    public void RemoveElement(ElementViewModel vm)
    {
        Model.Elements.Remove(vm.Model);
        Elements.Remove(vm);
    }
}
