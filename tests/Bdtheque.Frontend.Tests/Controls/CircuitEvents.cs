using AngleSharp.Dom;
using Bdtheque.Frontend.Components.Controls;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Time.Testing;

namespace Bdtheque.Frontend.Tests.Controls;

/// <summary>
/// The events a test raises on an interactive component, as its circuit receives them: each one is handled on the renderer's
/// dispatcher, and the test awaits the end of its handling, which bUnit signals once the rendering it causes is done.
/// </summary>
/// <remarks>
/// A handler that awaits the clock of the application (e.g. the typing pause of a <see cref="RecordPicker"/>) resumes on
/// the renderer's dispatcher, apart from the test: no polling of the markup, nor of the clock, can know when it is done,
/// and a loop that spins until it is competes for the processors with the very work it awaits (#344).
/// </remarks>
internal static class CircuitEvents
{
    // The handling of an event takes milliseconds: the limit only stops a test whose event would never end, and says so.
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Raises an event on the element found by <paramref name="selector"/>, found and raised on the renderer's dispatcher,
    /// where the component handles it at once, up to its first await (e.g. the pause of the typing, then awaited on the
    /// clock). Returns the task of its handling, which ends once the rendering it causes is done.
    /// </summary>
    public static async Task<Task> RaiseAsync<TComponent>(
        this IRenderedComponent<TComponent> component, string selector, Func<IElement, Task> raise)
        where TComponent : IComponent
    {
        Task handling = null!;
        // An action, not a function returning the task: the dispatcher would await the whole handling.
        await component.InvokeAsync(() => { handling = raise(component.Find(selector)); }).EndAsync($"Raising the event on {selector}");
        return handling;
    }

    /// <summary>Awaits the end of the handling of an event, failing explicitly if it does not end.</summary>
    public static async Task EndAsync(this Task handling, string what)
    {
        try
        {
            await handling.WaitAsync(Limit);
        }
        catch (TimeoutException)
        {
            Assert.Fail($"{what} did not end within {Limit.TotalSeconds} s.");
        }
    }

    // The other events of the tests, each raised and awaited until its handling ends: an event raised by the synchronous
    // methods of bUnit runs at once only on an idle dispatcher, and is otherwise queued, unawaited, after the end of an
    // earlier handling that the dispatcher is still completing (#344).

    public static async Task ClickAsync<TComponent>(this IRenderedComponent<TComponent> component, string selector)
        where TComponent : IComponent =>
        await (await component.RaiseAsync(selector, element => element.ClickAsync(new MouseEventArgs()))).EndAsync($"The click on {selector}");

    public static async Task KeyDownAsync<TComponent>(this IRenderedComponent<TComponent> component, string selector, string key)
        where TComponent : IComponent =>
        await (await component.RaiseAsync(selector, element => element.KeyDownAsync(new KeyboardEventArgs { Key = key }))).EndAsync($"The key {key} on {selector}");

    public static async Task BlurAsync<TComponent>(this IRenderedComponent<TComponent> component, string selector)
        where TComponent : IComponent =>
        await (await component.RaiseAsync(selector, element => element.BlurAsync(new FocusEventArgs()))).EndAsync($"Leaving {selector}");

    /// <summary>
    /// Types <paramref name="text"/> in the <see cref="RecordPicker"/> found by <paramref name="selector"/>, then lets the
    /// typing pause on <paramref name="clock"/>. Returns the task of the keystroke, which ends once the field has searched
    /// the text and rendered its suggestions.
    /// </summary>
    public static async Task<Task> TypeAsync<TComponent>(
        this IRenderedComponent<TComponent> component, string selector, string text, FakeTimeProvider clock)
        where TComponent : IComponent
    {
        // The pause is awaited on the clock as the keystroke is handled, before RaiseAsync returns: a single move of the
        // clock ends it.
        var keystroke = await component.RaiseAsync(selector, field => field.InputAsync(new ChangeEventArgs { Value = text }));
        clock.Advance(RecordPicker.TypingPause);
        return keystroke;
    }
}
