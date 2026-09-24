import React, { useState, useEffect } from 'react';
import { View, Text, StatusBar, TextInput, TouchableOpacity, StyleSheet, ActivityIndicator } from 'react-native';
import { NavigationContainer } from '@react-navigation/native';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { AuthProvider, useAuth } from './context/AuthContext';
import { DashboardScreen } from './screens/DashboardScreen';
import { StreamShareScreen } from './screens/StreamShareScreen';
import { KnowledgeMapScreen } from './screens/KnowledgeMapScreen';
import { RecommendationsScreen } from './screens/RecommendationsScreen';
import { ProfileSettingsScreen } from './screens/ProfileSettingsScreen';
import { QuickQuizModal } from './components/QuickQuizModal';
import { KnowledgeCheckDetail } from './types';
import { colors, spacing, typography } from './styles/theme';
import * as Linking from 'expo-linking';
import { mobileApi } from './services/api';
import {
  LayoutDashboard,
  Share2,
  Network,
  Compass,
  Sliders,
  Shield,
  Lock,
  Mail,
  ArrowRight,
} from 'lucide-react-native';

const Tab = createBottomTabNavigator();

// Auth Screen for Mobile
const AuthScreen: React.FC = () => {
  const { login, register } = useAuth();
  const [isRegister, setIsRegister] = useState<boolean>(false);
  const [email, setEmail] = useState<string>('test@scrollguardian.app');
  const [password, setPassword] = useState<string>('Password123!');
  const [fullName, setFullName] = useState<string>('Alex Morgan');
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async () => {
    setIsLoading(true);
    setError(null);
    try {
      if (isRegister) {
        await register(email.trim(), password, fullName.trim());
      } else {
        await login(email.trim(), password);
      }
    } catch (err: any) {
      setError(err.message || 'Authentication failed');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <View style={authStyles.container}>
      <View style={authStyles.card}>
        <View style={authStyles.logo}>
          <Shield color="#fff" size={28} />
        </View>
        <Text style={[typography.h1, { textAlign: 'center', marginTop: 12 }]}>
          {isRegister ? 'Create Account' : 'Scroll Guardian'}
        </Text>
        <Text style={[typography.caption, { textAlign: 'center', marginTop: 4, marginBottom: 20 }]}>
          Intentional digital habits & knowledge retention
        </Text>

        {error && (
          <View style={authStyles.errorBox}>
            <Text style={authStyles.errorText}>{error}</Text>
          </View>
        )}

        {isRegister && (
          <View style={{ marginBottom: 12 }}>
            <Text style={authStyles.label}>Full Name</Text>
            <TextInput
              style={authStyles.input}
              value={fullName}
              onChangeText={setFullName}
              placeholder="Alex Morgan"
              placeholderTextColor={colors.textMuted}
            />
          </View>
        )}

        <View style={{ marginBottom: 12 }}>
          <Text style={authStyles.label}>Email Address</Text>
          <TextInput
            style={authStyles.input}
            value={email}
            onChangeText={setEmail}
            placeholder="alex@example.com"
            placeholderTextColor={colors.textMuted}
            autoCapitalize="none"
          />
        </View>

        <View style={{ marginBottom: 20 }}>
          <Text style={authStyles.label}>Password</Text>
          <TextInput
            style={authStyles.input}
            value={password}
            onChangeText={setPassword}
            placeholder="••••••••"
            placeholderTextColor={colors.textMuted}
            secureTextEntry
          />
        </View>

        <TouchableOpacity style={authStyles.btn} onPress={handleSubmit} disabled={isLoading}>
          {isLoading ? (
            <ActivityIndicator color="#fff" size="small" />
          ) : (
            <>
              <Text style={authStyles.btnText}>{isRegister ? 'Create Account' : 'Sign In'}</Text>
              <ArrowRight color="#fff" size={16} />
            </>
          )}
        </TouchableOpacity>

        <TouchableOpacity
          style={{ marginTop: 16, alignItems: 'center' }}
          onPress={() => setIsRegister(!isRegister)}
        >
          <Text style={{ color: colors.primary, fontSize: 13, fontWeight: '600' }}>
            {isRegister ? 'Already have an account? Sign In' : 'New user? Create an account'}
          </Text>
        </TouchableOpacity>
      </View>
    </View>
  );
};

// Main Mobile Tab Navigation
const MainTabs: React.FC = () => {
  const [activeQuiz, setActiveQuiz] = useState<KnowledgeCheckDetail | null>(null);
  const [isQuizVisible, setIsQuizVisible] = useState<boolean>(false);

  // Deep linking / Share Intent listener
  useEffect(() => {
    const handleUrl = async (event: { url: string }) => {
      const parsed = Linking.parse(event.url);
      if (parsed.queryParams && parsed.queryParams.url) {
        const sharedUrl = decodeURIComponent(parsed.queryParams.url as string);
        await mobileApi.ingestContent(sharedUrl);
        alert(`Shared video ingested successfully: ${sharedUrl}`);
      }
    };

    Linking.getInitialURL().then((url) => {
      if (url) handleUrl({ url });
    });

    const subscription = Linking.addEventListener('url', handleUrl);
    return () => subscription.remove();
  }, []);

  const openQuiz = (quiz: KnowledgeCheckDetail) => {
    setActiveQuiz(quiz);
    setIsQuizVisible(true);
  };

  return (
    <>
      <Tab.Navigator
        screenOptions={{
          headerShown: false,
          tabBarStyle: {
            backgroundColor: colors.bgCard,
            borderTopColor: colors.borderSubtle,
            height: 60,
            paddingBottom: 8,
            paddingTop: 8,
          },
          tabBarActiveTintColor: colors.primary,
          tabBarInactiveTintColor: colors.textMuted,
          tabBarLabelStyle: { fontSize: 11, fontWeight: '600' },
        }}
      >
        <Tab.Screen
          name="Dashboard"
          options={{
            tabBarIcon: ({ color, size }) => <LayoutDashboard color={color} size={size} />,
          }}
        >
          {(props) => <DashboardScreen {...props} onOpenQuiz={openQuiz} />}
        </Tab.Screen>

        <Tab.Screen
          name="Share"
          component={StreamShareScreen}
          options={{
            tabBarIcon: ({ color, size }) => <Share2 color={color} size={size} />,
          }}
        />

        <Tab.Screen
          name="Knowledge"
          options={{
            tabBarIcon: ({ color, size }) => <Network color={color} size={size} />,
          }}
        >
          {(props) => <KnowledgeMapScreen {...props} onOpenQuiz={openQuiz} />}
        </Tab.Screen>

        <Tab.Screen
          name="Explore"
          component={RecommendationsScreen}
          options={{
            tabBarIcon: ({ color, size }) => <Compass color={color} size={size} />,
          }}
        />

        <Tab.Screen
          name="Settings"
          component={ProfileSettingsScreen}
          options={{
            tabBarIcon: ({ color, size }) => <Sliders color={color} size={size} />,
          }}
        />
      </Tab.Navigator>

      <QuickQuizModal
        visible={isQuizVisible}
        check={activeQuiz}
        onClose={() => setIsQuizVisible(false)}
        onCompleted={() => {
          setIsQuizVisible(false);
          setActiveQuiz(null);
        }}
      />
    </>
  );
};

const NavigationWrapper: React.FC = () => {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <View style={{ flex: 1, backgroundColor: colors.bgPrimary, justifyContent: 'center', alignItems: 'center' }}>
        <ActivityIndicator color={colors.primary} size="large" />
      </View>
    );
  }

  return (
    <NavigationContainer>
      <StatusBar barStyle="light-content" backgroundColor={colors.bgPrimary} />
      {isAuthenticated ? <MainTabs /> : <AuthScreen />}
    </NavigationContainer>
  );
};

export default function App() {
  return (
    <AuthProvider>
      <NavigationWrapper />
    </AuthProvider>
  );
}

const authStyles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.bgPrimary,
    justifyContent: 'center',
    padding: spacing.lg,
  },
  card: {
    backgroundColor: colors.bgCard,
    borderWidth: 1,
    borderColor: colors.borderSubtle,
    borderRadius: 20,
    padding: spacing.xl,
  },
  logo: {
    width: 52,
    height: 52,
    borderRadius: 16,
    backgroundColor: colors.primary,
    alignSelf: 'center',
    alignItems: 'center',
    justifyContent: 'center',
  },
  label: { fontSize: 12, fontWeight: '600', color: colors.textSecondary, marginBottom: 4 },
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
  btn: {
    backgroundColor: colors.primary,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    paddingVertical: 12,
    borderRadius: 12,
  },
  btnText: { color: '#fff', fontSize: 14, fontWeight: '700' },
  errorBox: {
    backgroundColor: 'rgba(244, 63, 94, 0.15)',
    borderColor: 'rgba(244, 63, 94, 0.3)',
    borderWidth: 1,
    borderRadius: 8,
    padding: 8,
    marginBottom: 12,
  },
  errorText: { color: colors.rose, fontSize: 12 },
});
