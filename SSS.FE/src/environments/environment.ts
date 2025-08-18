export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api',
  appName: 'SSS Employee Management',
  version: '2.0.0',
  features: {
    realTimeNotifications: true,
    darkMode: true,
    animations: true,
    analytics: false,
    offlineMode: false
  },
  api: {
    timeout: 30000,
    retryAttempts: 3,
    retryDelay: 1000
  },
  ui: {
    pageSize: 10,
    autoSave: true,
    autoSaveInterval: 30000, // 30 seconds
    theme: 'light',
    animation: {
      duration: 300,
      easing: 'ease-in-out'
    }
  },
  storage: {
    tokenKey: 'sss_auth_token',
    refreshTokenKey: 'sss_refresh_token',
    userKey: 'sss_current_user',
    themeKey: 'sss_theme',
    settingsKey: 'sss_settings'
  },
  logging: {
    level: 'debug',
    enableConsole: true,
    enableRemote: false
  }
};
