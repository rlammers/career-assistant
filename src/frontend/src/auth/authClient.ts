import { InteractionRequiredAuthError, PublicClientApplication } from '@azure/msal-browser';
import { apiTokenRequest, authenticationEnabled, msalConfig } from './authConfig';

export const msalInstance = new PublicClientApplication(msalConfig);

interface ApiAccessTokenOptions {
  forceRefresh?: boolean;
}

export const getApiAccessToken = async (
  options: ApiAccessTokenOptions = {},
): Promise<string | null> => {
  if (!authenticationEnabled) return null;

  const account = msalInstance.getActiveAccount() ?? msalInstance.getAllAccounts()[0];
  if (!account) throw new Error('Sign in is required before calling the API.');

  try {
    const response = await msalInstance.acquireTokenSilent({
      ...apiTokenRequest,
      account,
      forceRefresh: options.forceRefresh ?? false,
    });
    return response.accessToken;
  } catch (error) {
    if (error instanceof InteractionRequiredAuthError) {
      await msalInstance.acquireTokenRedirect(apiTokenRequest);
    }

    throw error;
  }
};
