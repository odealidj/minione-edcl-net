describe('Dashboard Page (Mocked API)', () => {
  beforeEach(() => {
    // Bypass UI login by setting the token directly in localStorage
    const fakeJwt = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJhZG1pbkBlZGNsLmNvbSIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IkFETUlOIn0K.fake-signature';
    window.localStorage.setItem('access_token', fakeJwt);
    
    cy.visit('/dashboard');
  });

  it('should render the welcome banner correctly', () => {
    cy.contains('Hello, Admin!').should('be.visible');
    cy.contains('Welcome to the Integrated Delivery Control System').should('be.visible');
    cy.contains('View Live Map').should('be.visible');
  });

  it('should navigate to monitoring when clicking View Live Map', () => {
    cy.contains('View Live Map').click();
    cy.url().should('include', '/operations/monitoring');
  });
});
