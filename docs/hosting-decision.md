# Hosting decision

Status: **not decided; provider-specific implementation is paused.**

Use this document to record the next hosting decision before adding or
extending cloud infrastructure. Existing Azure material is historical reference
evidence, not an active default. The owner reports that the prior Azure
subscription expired and was deleted; this has not been independently
verified here. A future Azure choice would require a new deployment and fresh
validation, not resumption of the old environment. This document does not
prefer Azure, AWS, or another provider.

## Required decision criteria

Evaluate each viable option against the same expected owner-only demo workload:

| Area | Questions to answer |
| --- | --- |
| Compute | Which managed container or application service runs the existing frontend and API images with minimal operational work? |
| Database | Which managed relational provider fits EF Core, migrations, expected idle time, backup and restore, and the cost ceiling? |
| Networking | How are TLS, ingress, backend isolation, outbound access, DNS, and database access constrained? |
| Infrastructure as code | Which supported tool reproduces the environment, supports reviewable previews, and enables complete teardown? |
| Identity | How does the existing Microsoft Entra SPA/API flow integrate, and what would justify replacing it? |
| Secrets | Where are database credentials and provider secrets stored, rotated, and exposed to workloads without entering images or source control? |
| Cost | What are the idle and bounded-test costs, free allowances, budget alerts, and likely cost surprises? |
| Operations | How are migrations, health checks, logs, monitoring, rollback, backup, restore, and incident cleanup performed? |
| Teardown | Can all provider resources be inventoried and removed without deleting unrelated identity or local-development state? |

## Decision gate

Before implementation resumes, record:

- the selected hosting and database services;
- the rejected alternatives and decisive trade-offs;
- the identity approach, including whether Microsoft Entra is retained;
- the infrastructure-as-code and migration strategy;
- the expected monthly and bounded-test cost controls;
- the private verification, rollback, recovery, and teardown plan; and
- the milestone definition of done.

Until those items are recorded, maintain the local source and Docker Compose
workflows and make no new provider-specific infrastructure changes.
