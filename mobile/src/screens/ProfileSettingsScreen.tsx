import React, { useState } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  ScrollView,
  StyleSheet,
  Alert,
} from 'react-native';
import { colors, spacing, typography } from '../styles/theme';
import { useAuth } from '../context/AuthContext';
import { mobileApi } from '../services/api';
import { User, Shield, Server, LogOut, Check } from 'lucide-react-native';

export const ProfileSettingsScreen: React.FC = () => {
  const { user, logout } = useAuth();
  const [apiHost, setApiHost] = useState<string>('http://10.0.2.2:5000/api');
  const [isSavedHost, setIsSavedHost] = useState<boolean>(false);

  const handleSaveHost = async () => {
    if (!apiHost.trim()) return;
    await mobileApi.setBaseUrl(apiHost.trim());
    setIsSavedHost(true);
    setTimeout(() => setIsSavedHost(false), 2500);
  };

  const handleLogout = () => {
    Alert.alert('Sign Out', 'Are you sure you want to sign out?', [
      { text: 'Cancel', style: 'cancel' },
      { text: 'Sign Out', style: 'destructive', onPress: logout },
    ]);
  };

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <View style={styles.header}>
        <Text style={typography.h1}>Account & Settings</Text>
        <Text style={[typography.caption, { marginTop: 4 }]}>
          Manage your identity, server connection, and preferences
        </Text>
      </View>

      {/* User Info Card */}
      <View style={styles.card}>
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: 12 }}>
          <View style={styles.avatar}>
            <User color={colors.primary} size={24} />
          </View>
          <View style={{ flex: 1 }}>
            <Text style={typography.h3}>{user?.fullName || 'User'}</Text>
            <Text style={typography.caption}>{user?.email}</Text>
            <View style={styles.planBadge}>
              <Text style={styles.planBadgeText}>
                {user?.plan === 2 ? 'Scroll Guardian Pro' : 'Free Tier'}
              </Text>
            </View>
          </View>
        </View>
      </View>

      {/* API Connection Host */}
      <View style={[styles.card, { marginTop: spacing.md }]}>
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6, marginBottom: 8 }}>
          <Server color={colors.primary} size={16} />
          <Text style={typography.h3}>Backend Server URL</Text>
        </View>
        <Text style={[typography.caption, { marginBottom: 10 }]}>
          Configure your backend API host (use 10.0.2.2 for Android emulator or your local WiFi IP).
        </Text>
        <TextInput
          style={styles.input}
          value={apiHost}
          onChangeText={setApiHost}
          placeholder="http://10.0.2.2:5000/api"
          placeholderTextColor={colors.textMuted}
          autoCapitalize="none"
        />
        <TouchableOpacity style={styles.saveHostBtn} onPress={handleSaveHost}>
          {isSavedHost ? (
            <>
              <Check color="#fff" size={16} />
              <Text style={styles.saveHostText}>Saved</Text>
            </>
          ) : (
            <Text style={styles.saveHostText}>Update Server URL</Text>
          )}
        </TouchableOpacity>
      </View>

      {/* Sign Out */}
      <TouchableOpacity style={styles.logoutBtn} onPress={handleLogout}>
        <LogOut color={colors.rose} size={18} />
        <Text style={styles.logoutText}>Sign Out of Scroll Guardian</Text>
      </TouchableOpacity>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bgPrimary },
  content: { padding: spacing.lg, paddingBottom: 40 },
  header: { marginBottom: spacing.lg },
  card: {
    backgroundColor: colors.bgCard,
    borderWidth: 1,
    borderColor: colors.borderSubtle,
    borderRadius: 16,
    padding: spacing.lg,
  },
  avatar: {
    width: 48,
    height: 48,
    borderRadius: 14,
    backgroundColor: 'rgba(59, 130, 246, 0.15)',
    borderWidth: 1,
    borderColor: 'rgba(59, 130, 246, 0.3)',
    alignItems: 'center',
    justifyContent: 'center',
  },
  planBadge: {
    backgroundColor: 'rgba(16, 185, 129, 0.15)',
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: 6,
    alignSelf: 'flex-start',
    marginTop: 6,
  },
  planBadgeText: { color: colors.emerald, fontSize: 10, fontWeight: '700' },
  input: {
    backgroundColor: colors.bgInput,
    borderColor: colors.borderSubtle,
    borderWidth: 1,
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 10,
    color: colors.textPrimary,
    fontSize: 13,
  },
  saveHostBtn: {
    backgroundColor: colors.primary,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    paddingVertical: 10,
    borderRadius: 10,
    marginTop: spacing.md,
  },
  saveHostText: { color: '#fff', fontSize: 13, fontWeight: '700' },
  logoutBtn: {
    backgroundColor: 'rgba(244, 63, 94, 0.1)',
    borderColor: 'rgba(244, 63, 94, 0.3)',
    borderWidth: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    paddingVertical: 14,
    borderRadius: 14,
    marginTop: spacing.xl,
  },
  logoutText: { color: colors.rose, fontSize: 14, fontWeight: '700' },
});
