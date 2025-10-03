import { config } from './config';

export interface RouteConfig {
  id: number;
  routeId: string;
  clusterId: string;
  match: string;
  order: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ClusterConfig {
  id: number;
  clusterId: string;
  destinationAddress: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface User {
  id: number;
  username: string;
  email: string;
  isEnabled: boolean;
  createdAt: string;
  updatedAt: string;
  lastLoginAt?: string;
  activeSessions?: number;
  sessions?: SessionToken[];
}

export interface SessionToken {
  id: number;
  tokenId: string;
  createdAt: string;
  lastAccessedAt: string;
  expiresAt: string;
  ipAddress: string;
  userAgent: string;
  isRevoked: boolean;
  isExpired: boolean;
}

export interface ClientCredential {
  id: number;
  clientId: string;
  description: string;
  isEnabled: boolean;
  createdAt: string;
  lastUsedAt?: string;
}

class ApiClient {
  private baseUrl: string;
  private clientId: string = '';
  private clientSecret: string = '';

  constructor() {
    this.baseUrl = config.apiBaseUrl;
  }

  setCredentials(clientId: string, clientSecret: string) {
    this.clientId = clientId;
    this.clientSecret = clientSecret;
    if (typeof window !== 'undefined') {
      localStorage.setItem('clientId', clientId);
      localStorage.setItem('clientSecret', clientSecret);
    }
  }

  loadCredentials() {
    if (typeof window !== 'undefined') {
      this.clientId = localStorage.getItem('clientId') || '';
      this.clientSecret = localStorage.getItem('clientSecret') || '';
    }
  }

  clearCredentials() {
    this.clientId = '';
    this.clientSecret = '';
    if (typeof window !== 'undefined') {
      localStorage.removeItem('clientId');
      localStorage.removeItem('clientSecret');
    }
  }

  hasCredentials(): boolean {
    return !!(this.clientId && this.clientSecret);
  }

  private getAuthHeader(): string {
    return 'Basic ' + btoa(`${this.clientId}:${this.clientSecret}`);
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`;
    const headers = {
      'Content-Type': 'application/json',
      Authorization: this.getAuthHeader(),
      ...options.headers,
    };

    const response = await fetch(url, {
      ...options,
      headers,
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ error: 'Unknown error' }));
      throw new Error(error.error || error.message || `HTTP ${response.status}`);
    }

    return response.json();
  }

  // Routes
  async getRoutes(): Promise<RouteConfig[]> {
    return this.request<RouteConfig[]>('/admin/routes');
  }

  async createRoute(route: Omit<RouteConfig, 'id' | 'createdAt' | 'updatedAt'>): Promise<RouteConfig> {
    return this.request<RouteConfig>('/admin/routes', {
      method: 'POST',
      body: JSON.stringify(route),
    });
  }

  async updateRoute(id: number, route: Omit<RouteConfig, 'id' | 'createdAt' | 'updatedAt'>): Promise<RouteConfig> {
    return this.request<RouteConfig>(`/admin/routes/${id}`, {
      method: 'PUT',
      body: JSON.stringify(route),
    });
  }

  async deleteRoute(id: number): Promise<{ message: string }> {
    return this.request<{ message: string }>(`/admin/routes/${id}`, {
      method: 'DELETE',
    });
  }

  // Clusters
  async getClusters(): Promise<ClusterConfig[]> {
    return this.request<ClusterConfig[]>('/admin/clusters');
  }

  async createCluster(cluster: Omit<ClusterConfig, 'id' | 'createdAt' | 'updatedAt'>): Promise<ClusterConfig> {
    return this.request<ClusterConfig>('/admin/clusters', {
      method: 'POST',
      body: JSON.stringify(cluster),
    });
  }

  async updateCluster(id: number, cluster: Omit<ClusterConfig, 'id' | 'createdAt' | 'updatedAt'>): Promise<ClusterConfig> {
    return this.request<ClusterConfig>(`/admin/clusters/${id}`, {
      method: 'PUT',
      body: JSON.stringify(cluster),
    });
  }

  async deleteCluster(id: number): Promise<{ message: string }> {
    return this.request<{ message: string }>(`/admin/clusters/${id}`, {
      method: 'DELETE',
    });
  }

  // Users
  async getUsers(): Promise<User[]> {
    return this.request<User[]>('/admin/users');
  }

  async getUser(id: number): Promise<User> {
    return this.request<User>(`/admin/users/${id}`);
  }

  async enableUser(id: number): Promise<{ message: string }> {
    return this.request<{ message: string }>(`/admin/users/${id}/enable`, {
      method: 'POST',
    });
  }

  async disableUser(id: number): Promise<{ message: string }> {
    return this.request<{ message: string }>(`/admin/users/${id}/disable`, {
      method: 'POST',
    });
  }

  // Sessions
  async getUserSessions(userId: number): Promise<SessionToken[]> {
    return this.request<SessionToken[]>(`/admin/users/${userId}/sessions`);
  }

  async revokeSession(sessionId: number): Promise<{ message: string }> {
    return this.request<{ message: string }>(`/admin/sessions/${sessionId}/revoke`, {
      method: 'POST',
    });
  }

  async revokeAllUserSessions(userId: number): Promise<{ message: string }> {
    return this.request<{ message: string }>(`/admin/users/${userId}/sessions/revoke-all`, {
      method: 'POST',
    });
  }

  // Clients
  async getClients(): Promise<ClientCredential[]> {
    return this.request<ClientCredential[]>('/admin/clients');
  }

  async createClient(client: { clientId: string; clientSecret: string; description: string }): Promise<ClientCredential> {
    return this.request<ClientCredential>('/admin/clients', {
      method: 'POST',
      body: JSON.stringify(client),
    });
  }

  async enableClient(id: number): Promise<{ message: string }> {
    return this.request<{ message: string }>(`/admin/clients/${id}/enable`, {
      method: 'POST',
    });
  }

  async disableClient(id: number): Promise<{ message: string }> {
    return this.request<{ message: string }>(`/admin/clients/${id}/disable`, {
      method: 'POST',
    });
  }

  async deleteClient(id: number): Promise<{ message: string }> {
    return this.request<{ message: string }>(`/admin/clients/${id}`, {
      method: 'DELETE',
    });
  }

  // OAuth endpoints (for testing token handler pattern)
  async startOAuthLogin(redirectUri: string): Promise<{ authorizationUrl: string }> {
    return this.request<{ authorizationUrl: string }>('/oauth/login/start', {
      method: 'POST',
      body: JSON.stringify({ redirectUri }),
      headers: {
        'Content-Type': 'application/json',
      },
    });
  }

  async checkLoginStatus(): Promise<{ isLoggedIn: boolean; userId?: string }> {
    return this.request<{ isLoggedIn: boolean; userId?: string }>('/oauth/isloggedin', {
      method: 'GET',
    });
  }

  async logout(): Promise<{ success: boolean; message: string }> {
    return this.request<{ success: boolean; message: string }>('/oauth/logout', {
      method: 'POST',
    });
  }
}

export const apiClient = new ApiClient();
