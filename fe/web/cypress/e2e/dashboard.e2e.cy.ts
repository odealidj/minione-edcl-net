describe('Dashboard Page (Real API)', () => {
  beforeEach(() => {
    // Perform a real login before viewing the dashboard
    cy.visit('/login');
    cy.intercept('POST', '**/api/v1/web/auth/users/login').as('realLoginRequest');
    
    cy.get('input[formControlName="email"]').type('admin@edcl.com');
    cy.get('input[formControlName="password"]').type('password123');
    cy.get('button[type="submit"]').click();

    cy.wait('@realLoginRequest').then((interception) => {
      if (interception.response && interception.response.statusCode === 200) {
        // Successfully logged in
        cy.url().should('not.include', '/login');
      } else {
        // Skip test if backend cannot log in
        this.skip();
      }
    });
  });

  it('should render the welcome banner correctly using real data', () => {
    cy.contains('Hello').should('be.visible');
    cy.contains('Welcome to the Integrated Delivery Control System').should('be.visible');
  });
});
