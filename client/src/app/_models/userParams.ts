import { User } from './user';

export class UserParams {
  gender: string;
  minAge = 18;
  maxAge = 99;
  pageNumber = 1;
  pagesize = 5;
  orderBy = 'lastActive';

  constructor(user: User | null) {
    this.gender = user?.gender === 'female' ? 'male' : 'female';
  }
}
