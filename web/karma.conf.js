// Used by CI (ng test --karma-config karma.conf.js --browsers=ChromeHeadlessCI).
// Hosted runners need Chrome started without its sandbox; local runs can keep using plain ChromeHeadless.
module.exports = function (config) {
  config.set({
    frameworks: ['jasmine'],
    plugins: [
      require('karma-jasmine'),
      require('karma-chrome-launcher'),
      require('karma-jasmine-html-reporter'),
    ],
    customLaunchers: {
      ChromeHeadlessCI: {
        base: 'ChromeHeadless',
        flags: ['--no-sandbox', '--disable-gpu'],
      },
    },
  });
};
