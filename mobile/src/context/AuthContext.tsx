import React, { createContext, useContext, useState, useEffect } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { UserProfile } from '../types';
import { mobileApi } from '../services/api';

interface AuthContextType {
  user: UserProfile | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, fullName: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshProfile: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  const refreshProfile = async () => {
    try {
      const savedUser = await AsyncStorage.getItem('sg_mobile_user');
      const token = await AsyncStorage.getItem('sg_mobile_token');

      if (token && savedUser) {
        setUser(JSON.parse(savedUser));
      }

      if (token) {
        const profile = await mobileApi.getCurrentUser();
        setUser(profile);
        await AsyncStorage.setItem('sg_mobile_user', JSON.stringify(profile));
      }
    } catch {
      setUser(null);
      await mobileApi.clearTokens();
    }
  };

  useEffect(() => {
    const init = async () => {
      setIsLoading(true);
      await refreshProfile();
      setIsLoading(false);
    };
    init();
  }, []);

  const login = async (email: string, password: string) => {
    const res = await mobileApi.login(email, password);
    setUser(res.user);
  };

  const register = async (email: string, password: string, fullName: string) => {
    const res = await mobileApi.register(email, password, fullName);
    setUser(res.user);
  };

  const logout = async () => {
    await mobileApi.clearTokens();
    setUser(null);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        isAuthenticated: !!user,
        login,
        register,
        logout,
        refreshProfile,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
