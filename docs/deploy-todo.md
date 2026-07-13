# Private Azure Containers deployment TODO

Status: **basic Microsoft Entra authentication and server-side authorization are working locally; the next milestone is deploying that working configuration privately to Azure Containers.**

Public deployment is the following milestone and is out of scope for now. Its additional identity and public-ingress checks are in [`production-todo.md`](./production-todo.md).

## Private deployment

- [ ] Provision the Azure Containers resources with private ingress.
- [ ] Configure the frontend and API containers with the same authentication settings used by the working local workflow, using secure Azure configuration for values that must not be committed.
- [ ] Configure persistent database storage and the deployed frontend/API connection.
- [ ] Set `AI__Provider=Mock` and do not configure an OpenAI API key or other paid-provider secret.
- [ ] Deploy the frontend and API images.
- [ ] Configure health probes and verify both deployed containers are healthy.

## Private verification

- [ ] Verify an invited user can sign in and use the core profile, job, status, and mock-analysis workflow.
- [ ] Verify the deployed API rejects unauthenticated direct requests on protected routes.
- [ ] Verify private ingress cannot bypass API authorization through a proxy or sidecar address.
- [ ] Record the private deployment decision before making the application available to invited users.
