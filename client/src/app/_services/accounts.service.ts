import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { User } from '../_models/user';
import { map } from 'rxjs';
import { environment } from '../../environments/environment';
import { LikesService } from './likes.service';
import { PresenceService } from './presence.service';

/**
 * can inject into components
 * singletons, instantiated when app is initalized and disposed of when app is disposed of.
 * If we need something shared amongst multiple components, then this is good to use
 * good place to make http requests, so we inject service into componenent
 * that needs to make that request, and use the service rather than HttpClient directly
 */

@Injectable({
  providedIn: 'root',
})
export class AccountService {
  private http = inject(HttpClient);
  private likeService = inject(LikesService);
  private presenceService = inject(PresenceService);
  baseUrl = environment.apiUrl;
  // Signals allow us to notify consumers when a value changes
  // So when we login or logout we can update the currentUser, which is a signal, an update the current user
  // in other components that consume the currentUser from the account.services.
  currentUser = signal<User | null>(null);
  roles = computed(() => {
    const user = this.currentUser();
    if (user && user.token) {
      const role = JSON.parse(atob(user.token.split('.')[1])).role;
      return Array.isArray(role) ? role : [role];
    }
    return [];
  });

  // Observables are LAZY and we have to subscribe to them and tell them what we want to do next.
  // Generally you have to unsubscribe from an observable but with HTTP requests we know that they will always complete.
  // In any other circumstance with observables, we want to unsubscribe

  // Pipe method allows chaining operators to the observable returned by the http.post call.
  // Map is transforming the observable result (the user returned from the POST request)
  // If a user is returned, set the current user to the user
  login(model: any) {
    return this.http.post<User>(this.baseUrl + 'account/login', model).pipe(
      map((user) => {
        if (user) {
          this.setCurrentUser(user);
        }
      })
    );
  }

  register(model: any) {
    return this.http.post<User>(this.baseUrl + 'account/register', model).pipe(
      map((user) => {
        if (user) {
          this.setCurrentUser(user);
        }
        return user;
      })
    );
  }

  setCurrentUser(user: User) {
    localStorage.setItem('user', JSON.stringify(user));
    this.currentUser.set(user);
    this.likeService.getLikeIds();
    this.presenceService.createHubConnection(user);
  }

  logout() {
    localStorage.removeItem('user');
    this.currentUser.set(null);
    this.presenceService.stopHubConnection();
  }
}
