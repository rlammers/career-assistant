# Security follow-up index

Active private-demo security checks and follow-ups are maintained in
[`deploy-todo.md`](deploy-todo.md) and summarized in
[`security-review.md`](security-review.md). Do not duplicate their checklists
here.

Public-production work remains in
[`production-todo.md`](production-todo.md), including:

- public network and database access design;
- browser-edge and proxy validation;
- broader guest and unassigned-user verification;
- client-address and rate-limit behavior;
- backup, recovery, availability, logging, and cost controls; and
- a fresh security review against the final live configuration.

Treat a follow-up as blocking only when its affected surface is about to be
enabled or when it creates a credible risk to credentials, data,
authentication, cost, or recovery.
