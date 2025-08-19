# SSSFE

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 20.1.5.

## Development server

To start a local development server, run:
npm start
Once the server is running, open your browser and navigate to `http://localhost:50503/`. The application will automatically reload whenever you modify any of the source files.

### Running with HTTPS

To start the development server with HTTPS using a self-signed certificate, run:
npm run start:https
⚠️ You'll need to accept the self-signed certificate warning in your browser. Alternatively, you can generate your own certificate or use [mkcert](https://github.com/FiloSottile/mkcert) for a locally-trusted certificate.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:
ng generate component component-name
For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:
ng generate --help
## Building

To build the project run:
ng build
This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Karma](https://karma-runner.github.io) test runner, use the following command:
ng test
## Running end-to-end tests

For end-to-end (e2e) testing, run:
ng e2e
Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.

## Troubleshooting

- **Error: Module not found '../../core/services/notification.service'**  
  ✅ **FIXED** - Import path corrected to `../../../core/services/notification.service`

- **Error: ToastContainerComponent is not a directive, component, or pipe**  
  ✅ **FIXED** - Added proper @Component decorator

- **Error: Cannot resolve NotificationService injection token**  
  ✅ **FIXED** - NotificationService is now properly provided with `@Injectable({ providedIn: 'root' })`
