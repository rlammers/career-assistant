import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { MsalProvider } from '@azure/msal-react'
import './index.css'
import App from './App.tsx'
import { authenticationEnabled, getAuthenticationConfigurationError } from './auth/authConfig.ts'
import { msalInstance } from './auth/authClient.ts'

const configurationError = getAuthenticationConfigurationError();

if (configurationError) {
  throw new Error(configurationError);
}

if (authenticationEnabled) {
  await msalInstance.initialize();
}

const application = authenticationEnabled
  ? <MsalProvider instance={msalInstance}><App /></MsalProvider>
  : <App />;

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    {application}
  </StrictMode>,
)
