export const environment = {
  production: true,
  apiUrl: 'https://sss-api.yourdomain.com/api',
  websocketUrl: 'wss://sss-api.yourdomain.com',
  appName: 'SSS Employee Management',
  version: '2.0.0',
  features: {
    realTimeNotifications: true,
    darkMode: true,
    animations: true,
    analytics: true,
    offlineMode: true
  },
  api: {
    timeout: 30000,
    retryAttempts: 3,
    retryDelay: 1000
  },
  ui: {
    pageSize: 20,
    autoSave: true,
    autoSaveInterval: 30000,
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
    level: 'warn',
    enableConsole: false,
    enableRemote: true
  }
};
