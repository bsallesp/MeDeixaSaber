module.exports = function (config) {
  process.env.CHROME_BIN = 'C:\\\\Program Files\\\\BraveSoftware\\\\Brave-Browser\\\\Application\\\\brave.exe';

  config.set({
    basePath: '',
    frameworks: ['jasmine', '@angular-devkit/build-angular'],
    plugins: [
      require('karma-jasmine'),
      require('karma-chrome-launcher'),
      require('karma-jasmine-html-reporter'),
      require('karma-coverage'),
      require('@angular-devkit/build-angular/plugins/karma')
    ],
    client: { jasmine: {}, clearContext: false },
    reporters: ['progress', 'kjhtml'],
    coverageReporter: {
      dir: require('path').join(__dirname, './coverage/app'),
      subdir: '.',
      reporters: [{ type: 'html' }, { type: 'text-summary' }]
    },
    customLaunchers: {
      BraveHeadless: {
        base: 'Chrome',
        flags: ['--headless', '--disable-gpu', '--no-sandbox']
      }
    },
    browsers: ['BraveHeadless'],
    singleRun: true,
    restartOnFileChange: true
  });
};
