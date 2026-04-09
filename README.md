# Software Design Patterns

A comprehensive reference guide to software design patterns with C# examples. Patterns are organised into five categories covering the classic GoF catalogue, distributed systems, and microservices architecture.

## SOLID Design Principles

The patterns in this guide are built on the five **SOLID** principles — Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, and Dependency Inversion. These principles form the foundational guidelines for writing maintainable, extensible object-oriented code.

- 📄 [docs/solid.md](docs/solid.md) — full explanation of each principle
- 💻 [source/solid.cs](source/solid.cs) — annotated C# examples

---

## Categories

| # | Category | Patterns |
|---|----------|----------|
| 1 | [🏭 Creational](#-creational-patterns) | 5 |
| 2 | [🔄 Behavioral](#-behavioral-patterns) | 11 |
| 3 | [🏗️ Structural](#️-structural-patterns) | 9 |
| 4 | [🌐 Distributed Systems](#-distributed-systems-patterns) | 12 |
| 5 | [🔬 Microservices](#-microservices-design-patterns) | 9 |

---

## 🏭 Creational Patterns

> **Goal:** Control how objects are created — decoupling object construction from the code that uses them.

| Pattern | Intent | Docs | Code |
|---------|--------|------|------|
| Abstract Factory | Create families of related objects without specifying concrete classes | [abstract-factory.md](docs/creational/abstract-factory.md) | [abstract-factory.cs](source/creational/abstract-factory.cs) |
| Builder | Construct complex objects step-by-step, separating construction from representation | [builder.md](docs/creational/builder.md) | [builder.cs](source/creational/builder.cs) |
| Factory Method | Let subclasses decide which class to instantiate | [factory-method.md](docs/creational/factory-method.md) | [factory-method.cs](source/creational/factory-method.cs) |
| Prototype | Clone an existing object rather than constructing a new one from scratch | [prototype.md](docs/creational/prototype.md) | [prototype.cs](source/creational/prototype.cs) |
| Singleton | Ensure a class has only one instance and provide a global access point to it | [singleton.md](docs/creational/singleton.md) | [singleton.cs](source/creational/singleton.cs) |

---

## 🔄 Behavioral Patterns

> **Goal:** Define how objects communicate and distribute responsibilities between them.

| Pattern | Intent | Docs | Code |
|---------|--------|------|------|
| Chain of Responsibility | Pass a request along a chain of handlers until one handles it | [chain-of-responsibility.md](docs/behavioral/chain-of-responsibility.md) | [chain-of-responsibility.cs](source/behavioral/chain-of-responsibility.cs) |
| Command | Encapsulate a request as an object to support undo, queuing, and logging | [command.md](docs/behavioral/command.md) | [command.cs](source/behavioral/command.cs) |
| Interpreter | Define a grammar and an interpreter for a simple language | [interpreter.md](docs/behavioral/interpreter.md) | [interpreter.cs](source/behavioral/interpreter.cs) |
| Iterator | Sequentially access elements of a collection without exposing its internals | [iterator.md](docs/behavioral/iterator.md) | [iterator.cs](source/behavioral/iterator.cs) |
| Mediator | Route all inter-object communication through a central hub to reduce coupling | [mediator.md](docs/behavioral/mediator.md) | [mediator.cs](source/behavioral/mediator.cs) |
| Memento | Capture and restore an object's internal state without violating encapsulation | [memento.md](docs/behavioral/memento.md) | [memento.cs](source/behavioral/memento.cs) |
| Observer | Notify multiple dependents automatically when an object changes state | [observer.md](docs/behavioral/observer.md) | [observer.cs](source/behavioral/observer.cs) |
| State | Let an object alter its behaviour when its internal state changes | [state.md](docs/behavioral/state.md) | [state.cs](source/behavioral/state.cs) |
| Strategy | Define a family of interchangeable algorithms and select one at runtime | [strategy.md](docs/behavioral/strategy.md) | [strategy.cs](source/behavioral/strategy.cs) |
| Template Method | Define the skeleton of an algorithm in a base class, deferring steps to subclasses | [template-method.md](docs/behavioral/template-method.md) | [template-method.cs](source/behavioral/template-method.cs) |
| Visitor | Separate an algorithm from the object structure it operates on | [visitor.md](docs/behavioral/visitor.md) | [visitor.cs](source/behavioral/visitor.cs) |

---

## 🏗️ Structural Patterns

> **Goal:** Compose classes and objects into larger structures while keeping them flexible and efficient.

| Pattern | Intent | Docs | Code |
|---------|--------|------|------|
| Adapter | Convert one interface into another that clients expect | [adapter.md](docs/structural/adapter.md) | [adapter.cs](source/structural/adapter.cs) |
| Aggregate | Group related objects under a root entity that controls all access | [aggregate.md](docs/structural/aggregate.md) | [aggregate.cs](source/structural/aggregate.cs) |
| Bridge | Decouple an abstraction from its implementation so both can vary independently | [bridge.md](docs/structural/bridge.md) | [bridge.cs](source/structural/bridge.cs) |
| Composite | Compose objects into tree structures to represent part-whole hierarchies | [composite.md](docs/structural/composite.md) | [composite.cs](source/structural/composite.cs) |
| Decorator | Attach additional behaviour to an object dynamically | [decorator.md](docs/structural/decorator.md) | [decorator.cs](source/structural/decorator.cs) |
| Extensibility | Enable behaviour extension without modifying existing code (plug-in / hook model) | [extensibility.md](docs/structural/extensibility.md) | [extensibility.cs](source/structural/extensibility.cs) |
| Facade | Provide a simplified interface to a complex subsystem | [facade.md](docs/structural/facade.md) | [facade.cs](source/structural/facade.cs) |
| Flyweight | Share fine-grained objects to reduce memory when many similar objects are needed | [flyweight.md](docs/structural/flyweight.md) | [flyweight.cs](source/structural/flyweight.cs) |
| Proxy | Provide a surrogate that controls access to another object | [proxy.md](docs/structural/proxy.md) | [proxy.cs](source/structural/proxy.cs) |

---

## 🌐 Distributed Systems Patterns

> **Goal:** Handle coordination, consistency, and reliability challenges across distributed nodes.

| Pattern | Intent | Docs | Code |
|---------|--------|------|------|
| Clock-Bound Wait | Wait until a safe time window has passed before acting on bounded clock uncertainty | [clock-bound-wait.md](docs/distributed/clock-bound-wait.md) | [clock-bound-wait.cs](source/distributed/clock-bound-wait.cs) |
| Consistent Core | Keep a small, strongly consistent cluster that the rest of the system relies on | [consistent-core.md](docs/distributed/consistent-core.md) | [consistent-core.cs](source/distributed/consistent-core.cs) |
| Emergent Leader | Elect a cluster leader through voting rather than manual configuration | [emergent-leader.md](docs/distributed/emergent-leader.md) | [emergent-leader.cs](source/distributed/emergent-leader.cs) |
| Fixed Partitions | Divide the data keyspace into a fixed number of partitions assigned to nodes | [fixed-partitions.md](docs/distributed/fixed-partitions.md) | [fixed-partitions.cs](source/distributed/fixed-partitions.cs) |
| Low-Water Mark | Track the minimum log index all followers have acknowledged so old entries can be pruned | [low-water-mark.md](docs/distributed/low-water-mark.md) | [low-water-mark.cs](source/distributed/low-water-mark.cs) |
| Quorum | Require a majority of nodes to agree before an operation is considered committed | [quorum.md](docs/distributed/quorum.md) | [quorum.cs](source/distributed/quorum.cs) |
| Request Batch | Accumulate multiple requests and send them in a single network call | [request-batch.md](docs/distributed/request-batch.md) | [request-batch.cs](source/distributed/request-batch.cs) |
| Request Pipeline | Send multiple requests without waiting for prior responses to maximise throughput | [request-pipeline.md](docs/distributed/request-pipeline.md) | [request-pipeline.cs](source/distributed/request-pipeline.cs) |
| Lease | Grant time-limited exclusive ownership of a resource to prevent conflicts | [lease.md](docs/distributed/lease.md) | [lease.cs](source/distributed/lease.cs) |
| Leader and Followers | Elect one leader to coordinate writes; followers replicate the leader's log | [leader-and-followers.md](docs/distributed/leader-and-followers.md) | [leader-and-followers.cs](source/distributed/leader-and-followers.cs) |
| HeartBeat | Detect node failures by periodically exchanging alive signals | [heartbeat.md](docs/distributed/heartbeat.md) | [heartbeat.cs](source/distributed/heartbeat.cs) |
| Version Vector | Track causality in a distributed system using per-node version counters | [version-vector.md](docs/distributed/version-vector.md) | [version-vector.cs](source/distributed/version-vector.cs) |

---

## 🔬 Microservices Design Patterns

> **Goal:** Design, compose, and operate fine-grained services that are independently deployable and scalable.

| Pattern | Intent | Docs | Code |
|---------|--------|------|------|
| Decomposition by Business Capabilities | Define service boundaries along business capability lines | [decomposition-by-business-capabilities.md](docs/microservices/decomposition-by-business-capabilities.md) | [decomposition-by-business-capabilities.cs](source/microservices/decomposition-by-business-capabilities.cs) |
| Decomposition by Subdomain | Use DDD bounded contexts to define service boundaries | [decomposition-by-subdomain.md](docs/microservices/decomposition-by-subdomain.md) | [decomposition-by-subdomain.cs](source/microservices/decomposition-by-subdomain.cs) |
| Strangler | Incrementally replace a legacy system by routing traffic to new services | [strangler.md](docs/microservices/strangler.md) | [strangler.cs](source/microservices/strangler.cs) |
| API Gateway | Provide a single entry point that routes, aggregates, and handles cross-cutting concerns | [api-gateway.md](docs/microservices/api-gateway.md) | [api-gateway.cs](source/microservices/api-gateway.cs) |
| Aggregator | Compose a unified response by calling multiple downstream services | [aggregator.md](docs/microservices/aggregator.md) | [aggregator.cs](source/microservices/aggregator.cs) |
| Database per Service | Each service owns its private data store — no shared database | [database-per-service.md](docs/microservices/database-per-service.md) | [database-per-service.cs](source/microservices/database-per-service.cs) |
| Saga | Manage long-running distributed transactions via a sequence of local transactions with compensating actions | [saga.md](docs/microservices/saga.md) | [saga.cs](source/microservices/saga.cs) |
| Health Check | Expose an endpoint reporting service liveness and readiness | [health-check.md](docs/microservices/health-check.md) | [health-check.cs](source/microservices/health-check.cs) |
| Circuit Breaker | Stop calling a failing downstream service to prevent cascading failures | [circuit-breaker.md](docs/microservices/circuit-breaker.md) | [circuit-breaker.cs](source/microservices/circuit-breaker.cs) |

---

## See Also

- [SOLID Design Principles](docs/solid.md) — foundational OOP principles underlying most patterns ([C# examples](source/solid.cs))
