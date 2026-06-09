// ============================================================
// Decomposition by Subdomain — C# Simulation
// ============================================================
//
// Intent: Use DDD Bounded Contexts to discover where the
// ubiquitous language changes and draw service boundaries
// there — each context has its own coherent model with no
// naming conflicts or semantic collisions.
//
// Three contexts demonstrated:
//   CRM context      — Customer = Contact with segment
//   Billing context  — Customer = Billing account with payment method
//   Shipping context — Customer = Recipient with address
// ============================================================

// ── CRM Bounded Context ────────────────────────────────────
namespace Crm
{
    record Contact(Guid Id, string FullName, string Email, string Segment);

    class ContactRepository
    {
        private readonly Dictionary<Guid, Contact> _store = new();

        public void Save(Contact c) { _store[c.Id] = c; Console.WriteLine($"  [CRM]      Saved contact {c.FullName} <{c.Email}>"); }
        public Contact? Find(Guid id) => _store.GetValueOrDefault(id);

        public void UpdateSegment(Guid id, string segment)
        {
            if (_store.TryGetValue(id, out var c))
                _store[id] = c with { Segment = segment };
        }
    }
}

// ── Billing Bounded Context ────────────────────────────────
namespace Billing
{
    record BillingAccount(Guid CustomerId, string PaymentToken, decimal Balance);

    class BillingRepository
    {
        private readonly Dictionary<Guid, BillingAccount> _store = new();

        public void Create(BillingAccount a) { _store[a.CustomerId] = a; Console.WriteLine($"  [Billing]  Created billing account {a.CustomerId} (token={a.PaymentToken})"); }
        public BillingAccount? Find(Guid customerId) => _store.GetValueOrDefault(customerId);

        public void Charge(Guid customerId, decimal amount)
        {
            if (_store.TryGetValue(customerId, out var a))
            {
                _store[customerId] = a with { Balance = a.Balance - amount };
                Console.WriteLine($"  [Billing]  Charged {amount:C} to {customerId} (balance={_store[customerId].Balance:C})");
            }
        }
    }
}

// ── Shipping Bounded Context ───────────────────────────────
namespace Shipping
{
    record Address(string Street, string City, string PostalCode, string Country);
    record Recipient(Guid OrderId, string FullName, Address DeliveryAddress);

    class ShipmentService
    {
        public string Dispatch(Recipient r)
        {
            var tracking = $"SHIP-{Guid.NewGuid():N}"[..10].ToUpper();
            Console.WriteLine($"  [Shipping] Dispatching {r.FullName} → {r.DeliveryAddress.City} | {tracking}");
            return tracking;
        }
    }
}

// ── Anti-Corruption Layer: translates across context boundaries ─
// When CRM creates a customer, downstream contexts are bootstrapped
// via domain events — each maps the concept to its own model.
class CustomerOnboardingService(
    Crm.ContactRepository crm,
    Billing.BillingRepository billing)
{
    public Guid OnboardCustomer(string name, string email, string paymentToken)
    {
        var id = Guid.NewGuid();

        // CRM context: contact
        crm.Save(new Crm.Contact(id, name, email, Segment: "new"));

        // Billing context: billing account (different model, same identity)
        billing.Create(new Billing.BillingAccount(id, paymentToken, Balance: 0));

        Console.WriteLine($"\n  Customer {id} onboarded across 2 bounded contexts.\n");
        return id;
    }
}

// ── Demo ────────────────────────────────────────────────
Console.WriteLine("=== Decomposition by Subdomain (DDD Bounded Contexts) ===\n");
Console.WriteLine("Same 'Customer' concept expressed differently in each context:\n");

var crmRepo = new Crm.ContactRepository();
var billingRepo = new Billing.BillingRepository();

var onboarding = new CustomerOnboardingService(crmRepo, billingRepo);
var customerId = onboarding.OnboardCustomer("Alice Smith", "alice@example.com", "tok_test_abc123");

// Each context uses its own model independently
billingRepo.Charge(customerId, 29.99m);
crmRepo.UpdateSegment(customerId, "active");

// Shipping knows about a Recipient — not a CRM Contact or Billing Account
var shipSvc = new Shipping.ShipmentService();
var tracking = shipSvc.Dispatch(new Shipping.Recipient(
    OrderId: Guid.NewGuid(),
    FullName: "Alice Smith",
    DeliveryAddress: new Shipping.Address("42 Elm St", "Springfield", "12345", "US")));

Console.WriteLine($"\nShipment tracking: {tracking}");
Console.WriteLine("\nEach bounded context has its own coherent model — no God Object.");
