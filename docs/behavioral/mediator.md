# 🤝 Mediator Pattern

The Mediator pattern defines an object that **encapsulates how a set of objects interact**. It promotes loose coupling by preventing objects from referring to each other directly, instead routing all communication through a central mediator.

## Intent

> Define an object that encapsulates how a set of objects interact. Mediator promotes loose coupling by keeping objects from referring to each other explicitly, and lets you vary their interaction independently.

## Problem

In a complex UI or collaborative system, components that need to react to each other's changes create a spider-web of direct references. Every component knows about — and depends on — every other component. Adding a new component means updating all existing ones.

### Bad Example

```csharp
// Tight coupling — each component holds references to every other
class LoginForm
{
    public UsernameField UsernameField { get; set; } = null!;
    public PasswordField PasswordField { get; set; } = null!;
    public SubmitButton  SubmitButton  { get; set; } = null!;

    public void OnUsernameChanged() =>
        SubmitButton.IsEnabled = PasswordField.HasText && UsernameField.HasText;
    // Each component must know the full graph of other components
}
```

### Good Example

```csharp
// ── Mediator interface ────────────────────────────────────
interface IMediator
{
    void Notify(object sender, string eventName);
}

// ── Base component ────────────────────────────────────────
abstract class UIComponent(IMediator mediator)
{
    protected void Trigger(string eventName) => mediator.Notify(this, eventName);
}

// ── Concrete components ───────────────────────────────────
class UsernameInput(IMediator m) : UIComponent(m)
{
    private string _value = "";
    public string Value
    {
        get => _value;
        set { _value = value; Trigger("UsernameChanged"); }
    }
}

class PasswordInput(IMediator m) : UIComponent(m)
{
    private string _value = "";
    public string Value
    {
        get => _value;
        set { _value = value; Trigger("PasswordChanged"); }
    }
}

class SubmitButton(IMediator m) : UIComponent(m)
{
    public bool IsEnabled { get; set; }
    public void Click() => Trigger("Submit");
}

// ── Concrete Mediator — owns all coordination logic ──────
class LoginFormMediator : IMediator
{
    public UsernameInput Username { get; }
    public PasswordInput Password { get; }
    public SubmitButton  Submit   { get; }

    public LoginFormMediator()
    {
        Username = new UsernameInput(this);
        Password = new PasswordInput(this);
        Submit   = new SubmitButton(this);
        Submit.IsEnabled = false;
    }

    public void Notify(object sender, string eventName)
    {
        switch (eventName)
        {
            case "UsernameChanged":
            case "PasswordChanged":
                Submit.IsEnabled =
                    !string.IsNullOrWhiteSpace(Username.Value) &&
                    !string.IsNullOrWhiteSpace(Password.Value);
                Console.WriteLine($"  Submit button enabled: {Submit.IsEnabled}");
                break;

            case "Submit":
                if (Submit.IsEnabled)
                    Console.WriteLine($"  Logging in as '{Username.Value}'...");
                break;
        }
    }
}

// ── Demo ──────────────────────────────────────────────────
var form = new LoginFormMediator();

form.Username.Value = "alice";     // Submit: false (no password yet)
form.Password.Value = "secret";    // Submit: true
form.Submit.Click();               // "Logging in as 'alice'..."
form.Username.Value = "";          // Submit: false again
form.Submit.Click();               // no action
```

## Key Takeaways

- Components are decoupled from each other — they only know the mediator interface.
- All coordination logic lives in one place (the mediator), making it easy to audit and change.
- Adding a new component means updating only the mediator, not every existing component.
- Real-world example: ASP.NET Core's MediatR library (CQRS request/response).

## When to Use

- Many components communicate in complex, many-to-many ways.
- You want to reuse components in different contexts without carrying their dependencies.
- Workflow or orchestration logic should be decoupled from the participants.

## When NOT to Use

- Only 2–3 objects interact — direct references are simpler and clearer.
- The mediator itself becomes a "God object" that knows too much — consider splitting it further.
