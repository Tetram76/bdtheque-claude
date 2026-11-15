using System.Runtime.CompilerServices;
using AngleSharp.Dom;
using Bdtheque.Frontend.Components.Controls;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Time.Testing;

namespace Bdtheque.Frontend.Tests.Controls;

// The one place where bUnit raises an event, on the dispatcher and awaited: the compiler refuses it everywhere else.
#pragma warning disable RS0030
/// <summary>
/// The events a test raises on a component, as a circuit receives them: each one is handled on the renderer's
/// dispatcher, and the test awaits the end of its handling, which bUnit signals once the rendering it causes is done.
/// </summary>
/// <remarks>
/// <para>
/// A handler that awaits the clock of the application (e.g. the typing pause of a <see cref="RecordPicker"/>) resumes on
/// the renderer's dispatcher, apart from the test: no polling of the markup, nor of the clock, can know when it is done,
/// and a loop that spins until it is competes for the processors with the very work it awaits (#344).
/// </para>
/// <para>
/// The synchronous methods of bUnit (<c>Click()</c>, <c>Change()</c>…) run the event at once only on an idle dispatcher,
/// and otherwise queue it, unawaited, after the end of an earlier handling that the dispatcher is still completing: the
/// test then reads the markup from before the event (#344). bUnit hosts every component on the same dispatcher, statically
/// rendered pages included, so this holds for any test of a component. The compiler refuses them in this project, as it
/// does bUnit's own asynchronous methods, which find the element apart from the dispatcher: an event is raised here only
/// (BannedSymbols.txt, choix-implementation/outillage.md).
/// </para>
/// <para>
/// Each event designates its element either by a selector, or by a function that finds it, called on the dispatcher, for an
/// element no selector names alone (e.g. the second of the visuals, the item of a menu by its text).
/// </para>
/// </remarks>
internal static class CircuitEvents
{
    // The handling of an event takes milliseconds: the limit only stops a test whose event would never end, and says so.
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

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

    public static Task ClickAsync<TComponent>(this IRenderedComponent<TComponent> component, string selector)
        where TComponent : IComponent => component.ClickAsync(() => component.Find(selector), selector);

    public static Task ClickAsync<TComponent>(
        this IRenderedComponent<TComponent> component, Func<IElement> find, [CallerArgumentExpression(nameof(find))] string element = "")
        where TComponent : IComponent =>
        component.RaiseAndEndAsync(find, $"The click on {element}", target => target.ClickAsync(new MouseEventArgs()));

    public static Task KeyDownAsync<TComponent>(this IRenderedComponent<TComponent> component, string selector, string key)
        where TComponent : IComponent =>
        component.RaiseAndEndAsync(() => component.Find(selector), $"The key {key} on {selector}", target => target.KeyDownAsync(new KeyboardEventArgs { Key = key }));

    public static Task BlurAsync<TComponent>(this IRenderedComponent<TComponent> component, string selector)
        where TComponent : IComponent =>
        component.RaiseAndEndAsync(() => component.Find(selector), $"Leaving {selector}", target => target.BlurAsync(new FocusEventArgs()));

    /// <summary>Changes the value of a field, a list or a box (<c>@onchange</c>, the default binding of a field).</summary>
    public static Task ChangeAsync<TComponent, TValue>(this IRenderedComponent<TComponent> component, string selector, TValue value)
        where TComponent : IComponent => component.ChangeAsync(() => component.Find(selector), value, selector);

    /// <inheritdoc cref="ChangeAsync{TComponent, TValue}(IRenderedComponent{TComponent}, string, TValue)"/>
    public static Task ChangeAsync<TComponent, TValue>(
        this IRenderedComponent<TComponent> component, Func<IElement> find, TValue value,
        [CallerArgumentExpression(nameof(find))] string element = "")
        where TComponent : IComponent =>
        component.RaiseAndEndAsync(find, $"The change of {element}", target => target.ChangeAsync(value));

    /// <summary>Types the value in a field that follows each keystroke (<c>@oninput</c>).</summary>
    public static Task InputAsync<TComponent>(this IRenderedComponent<TComponent> component, string selector, string value)
        where TComponent : IComponent => component.InputAsync(() => component.Find(selector), value, selector);

    /// <inheritdoc cref="InputAsync{TComponent}(IRenderedComponent{TComponent}, string, string)"/>
    public static async Task InputAsync<TComponent>(
        this IRenderedComponent<TComponent> component, Func<IElement> find, string value,
        [CallerArgumentExpression(nameof(find))] string element = "")
        where TComponent : IComponent =>
        await (await component.StartInputAsync(find, value, element)).EndAsync($"Typing in {element}");

    public static async Task SubmitAsync<TComponent>(this IRenderedComponent<TComponent> component, string selector)
        where TComponent : IComponent =>
        await (await component.StartSubmitAsync(selector)).EndAsync($"The submission of {selector}");

    /// <summary>
    /// Submits the form found by <paramref name="selector"/>, without awaiting the end of its handling, for a test that holds
    /// it (e.g. a saving held to see the form while it is in progress). Returns the task of the submission.
    /// </summary>
    public static Task<Task> StartSubmitAsync<TComponent>(this IRenderedComponent<TComponent> component, string selector)
        where TComponent : IComponent =>
        component.RaiseAsync(() => component.Find(selector), $"The submission of {selector}", target => target.SubmitAsync(new EventArgs()));

    /// <summary>Drags the element found by <paramref name="from"/> and drops it on the one found by <paramref name="to"/>.</summary>
    public static async Task DragAndDropAsync<TComponent>(
        this IRenderedComponent<TComponent> component, Func<IElement> from, Func<IElement> to,
        [CallerArgumentExpression(nameof(from))] string dragged = "", [CallerArgumentExpression(nameof(to))] string target = "")
        where TComponent : IComponent
    {
        await component.RaiseAndEndAsync(from, $"Dragging {dragged}", element => element.DragStartAsync(new DragEventArgs()));
        await component.RaiseAndEndAsync(to, $"Dropping on {target}", element => element.DropAsync(new DragEventArgs()));
    }

    /// <summary>
    /// Types <paramref name="value"/> in the field found by <paramref name="selector"/>, without awaiting the end of its
    /// handling, for a test that holds it up to its first await (e.g. keystrokes typed within the typing pause of a
    /// <see cref="RecordPicker"/>, the clock standing still). Returns the task of the keystroke.
    /// </summary>
    public static Task<Task> StartInputAsync<TComponent>(this IRenderedComponent<TComponent> component, string selector, string value)
        where TComponent : IComponent => component.StartInputAsync(() => component.Find(selector), value, selector);

    /// <summary>
    /// Types <paramref name="text"/> in the <see cref="RecordPicker"/> found by <paramref name="selector"/>, then lets the
    /// typing pause on <paramref name="clock"/>. Returns the task of the keystroke, which ends once the field has searched
    /// the text and rendered its suggestions.
    /// </summary>
    public static async Task<Task> TypeAsync<TComponent>(
        this IRenderedComponent<TComponent> component, string selector, string text, FakeTimeProvider clock)
        where TComponent : IComponent
    {
        // The pause is awaited on the clock as the keystroke is handled, before StartInputAsync returns: a single move of
        // the clock ends it.
        var keystroke = await component.StartInputAsync(selector, text);
        clock.Advance(RecordPicker.TypingPause);
        return keystroke;
    }

    private static Task<Task> StartInputAsync<TComponent>(
        this IRenderedComponent<TComponent> component, Func<IElement> find, string value, string element)
        where TComponent : IComponent =>
        component.RaiseAsync(find, $"Typing in {element}", target => target.InputAsync(new ChangeEventArgs { Value = value }));

    private static async Task RaiseAndEndAsync<TComponent>(
        this IRenderedComponent<TComponent> component, Func<IElement> find, string what, Func<IElement, Task> raise)
        where TComponent : IComponent =>
        await (await component.RaiseAsync(find, what, raise)).EndAsync(what);

    /// <summary>
    /// Raises an event on the element <paramref name="find"/> returns, found and raised on the renderer's dispatcher, where
    /// the component handles it at once, up to its first await (e.g. the pause of the typing, then awaited on the clock).
    /// Returns the task of its handling, which ends once the rendering it causes is done.
    /// </summary>
    private static async Task<Task> RaiseAsync<TComponent>(
        this IRenderedComponent<TComponent> component, Func<IElement> find, string what, Func<IElement, Task> raise)
        where TComponent : IComponent
    {
        Task handling = null!;
        // An action, not a function returning the task: the dispatcher would await the whole handling.
        await component.InvokeAsync(() => { handling = raise(find()); }).EndAsync($"{what}, raised on the dispatcher,");
        return handling;
    }
}
#pragma warning restore RS0030
