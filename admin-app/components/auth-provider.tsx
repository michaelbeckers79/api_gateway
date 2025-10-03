'use client';

import React, { createContext, useContext, useEffect, useState } from 'react';
import { apiClient } from '@/lib/api-client';

interface AuthContextType {
  isAuthenticated: boolean;
  login: (clientId: string, clientSecret: string) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  useEffect(() => {
    apiClient.loadCredentials();
    setIsAuthenticated(apiClient.hasCredentials());
  }, []);

  const login = (clientId: string, clientSecret: string) => {
    apiClient.setCredentials(clientId, clientSecret);
    setIsAuthenticated(true);
  };

  const logout = () => {
    apiClient.clearCredentials();
    setIsAuthenticated(false);
  };

  return (
    <AuthContext.Provider value={{ isAuthenticated, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
