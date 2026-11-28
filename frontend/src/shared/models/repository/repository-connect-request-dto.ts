export interface RepositoryConnectRequestDto {
  url: string;

  /** Empty keeps the token already saved. */
  token: string;
}
