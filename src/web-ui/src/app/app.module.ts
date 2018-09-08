import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';

import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';
import { CoreModule } from 'app/core';

@NgModule({
  imports: [
    BrowserModule,
    AppRoutingModule,
    CoreModule
  ],
  providers: [
      
  ],
  declarations: [AppComponent],
  bootstrap: [AppComponent]
})
export class AppModule { }
