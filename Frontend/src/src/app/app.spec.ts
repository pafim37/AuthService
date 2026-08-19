import { TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { provideRouter, RouterOutlet } from '@angular/router';
import { App } from './app';
import { AppHeaderComponent } from '../components/app-header-component/app-header-component';

@Component({
  selector: 'app-header-component',
  template: '<header data-testid="app-header"></header>',
})
class AppHeaderStubComponent {}

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    });

    TestBed.overrideComponent(App, {
      remove: {
        imports: [AppHeaderComponent],
      },
      add: {
        imports: [AppHeaderStubComponent],
      },
    });

    await TestBed.compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the app layout container', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.container')).toBeTruthy();
  });

  it('should render the app header', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="app-header"]')).toBeTruthy();
  });

  it('should render the router outlet', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const routerOutlet = fixture.debugElement.query(
      (debugElement) => debugElement.providerTokens.includes(RouterOutlet),
    );

    expect(routerOutlet).toBeTruthy();
  });
});
