export const environment = {
  production: false,
  apiUrl: 'https://localhost:7005/api',
  appName: 'SSS Employee Management',
  version: '1.0.0',
  defaultLanguage: 'vi',
  supportedLanguages: ['vi', 'en'],
  features: {
    enableNotifications: true,
    enableDarkMode: true,
    enableFileUpload: true,
    enableRealTimeUpdates: false,
    enableOfflineSupport: false
  },
  storage: {
    tokenKey: 'sss_access_token',
    refreshTokenKey: 'sss_refresh_token',
    userKey: 'sss_user_info',
    settingsKey: 'sss_user_settings'
  },
  api: {
    timeout: 30000,
    retryAttempts: 3,
    retryDelay: 1000
  },
  ui: {
    itemsPerPage: 10,
    debounceTime: 300,
    animationDuration: 300,
    toastDuration: 5000
  }
};
