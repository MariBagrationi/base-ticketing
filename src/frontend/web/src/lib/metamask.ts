export interface EthereumProvider {
  request(args: { method: string; params?: unknown[] }): Promise<unknown>;
  on?: (event: string, listener: (...args: unknown[]) => void) => void;
  removeListener?: (event: string, listener: (...args: unknown[]) => void) => void;
}

declare global {
  interface Window {
    ethereum?: EthereumProvider;
  }
}

export function getEthereumProvider() {
  if (typeof window === "undefined" || !window.ethereum) {
    throw new Error("MetaMask is not installed. Install the MetaMask extension and try again.");
  }
  return window.ethereum;
}

export async function requestMetaMaskAccounts() {
  const accounts = await getEthereumProvider().request({ method: "eth_requestAccounts" });
  if (!Array.isArray(accounts) || typeof accounts[0] !== "string") {
    throw new Error("MetaMask did not return an account.");
  }
  return accounts as string[];
}

export async function signMetaMaskMessage(address: string, message: string) {
  const signature = await getEthereumProvider().request({
    method: "personal_sign",
    params: [message, address],
  });
  if (typeof signature !== "string") throw new Error("MetaMask did not return a signature.");
  return signature;
}
