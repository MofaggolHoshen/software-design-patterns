// ============================================================
// Mediator Pattern — C# Example
// ============================================================
//
// Intent: Route communication between objects through a central
// mediator to reduce direct coupling.
//
// Key roles:
//   IMediator      — Mediator interface
//   UIComponent    — Abstract colleague
//   UsernameInput, PasswordInput, SubmitButton — Concrete colleagues
//   LoginFormMediator — Concrete Mediator (owns coordination logic)
// ============================================================

// ── Mediator interface ───────────────────────────────────
interface IMediator
{
    void Notify(object sender, string eventName);
}

// ── Abstract UI component ────────────────────────────────
abstract class UIComponent(IMediator mediator)
{
    // Components trigger events; they never call other components directly
    protected void Trigger(string eventName) => mediator.Notify(this, eventName);
}

// ── Concrete components ──────────────────────────────────
class UsernameInput(IMediator m) : UIComponent(m)
{
    private string _value = "";

    public string Value
    {
        get => _value;
        set { _value = value; Trigger("TextChanged"); }
    }
}

class PasswordInput(IMediator m) : UIComponent(m)
{
    private string _value = "";

    public string Value
    {
        get => _value;
        set { _value = value; Trigger("TextChanged"); }
    }
}

class SubmitButton(IMediator m) : UIComponent(m)
{
    public bool IsEnabled { get; set; }

    public void Click()
    {
        if (IsEnabled)
            Trigger("Submit");
        else
            Console.WriteLine("  [Button] Cannot submit — button is disabled.");
    }
}

// ── Concrete Mediator ────────────────────────────────────
class LoginFormMediator : IMediator
{
    public UsernameInput Username { get; }
    public PasswordInput Password { get; }
    public SubmitButton Submit { get; }

    public LoginFormMediator()
    {
        Username = new UsernameInput(this);
        Password = new PasswordInput(this);
        Submit = new SubmitButton(this) { IsEnabled = false };
    }

    public void Notify(object sender, string eventName)
    {
        switch (eventName)
        {
            case "TextChanged":
                Submit.IsEnabled =
                    !string.IsNullOrWhiteSpace(Username.Value) &&
                    !string.IsNullOrWhiteSpace(Password.Value);
                Console.WriteLine($"  [Mediator] Submit enabled: {Submit.IsEnabled}");
                break;

            case "Submit":
                Console.WriteLine($"  [Mediator] Authenticating '{Username.Value}'...");
                // Trigger business logic — clear password on success, etc.
                Password.Value = "";
                break;
        }
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Mediator Pattern ===\n");

var form = new LoginFormMediator();

Console.WriteLine("Type username only:");
form.Username.Value = "alice";
form.Submit.Click();             // disabled — no password

Console.WriteLine("\nType password:");
form.Password.Value = "secret";
form.Submit.Click();             // enabled — triggers login

Console.WriteLine("\nClear username:");
form.Username.Value = "";
form.Submit.Click();             // disabled again
