import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { APP_BASE_HREF } from '@angular/common';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { ApplicationInsightsModule, AppInsightsService } from '@markpieszak/ng-application-insights';
import { ORIGIN_URL, REQUEST } from '@nguniversal/aspnetcore-engine';
import { AppModuleShared } from './app.module';
import { AppComponent } from './app.component';
import { BrowserTransferStateModule } from '@angular/platform-browser';
import { BrowserPrebootModule } from 'preboot/browser';

export function getOriginUrl() {
  return window.location.origin;
}

export function getRequest() {
  // the Request object only lives on the server
  return { cookie: document.cookie };
}

@NgModule({
  bootstrap: [AppComponent],
  imports: [
    BrowserPrebootModule.replayEvents(),
    BrowserAnimationsModule,
    ApplicationInsightsModule.forRoot({
      instrumentationKey: window['TRANSFER_CACHE']['AppInsightsId']
    }),

    // Our Common AppModule
    AppModuleShared

  ],
  providers: [
    {
      // We need this for our Http calls since they'll be using an ORIGIN_URL provided in main.server
      // (Also remember the Server requires Absolute URLs)
      provide: ORIGIN_URL,
      useFactory: (getOriginUrl)
    }, {
      // The server provides these in main.server
      provide: REQUEST,
      useFactory: (getRequest)
    }
  ]
})
export class AppModule { }
