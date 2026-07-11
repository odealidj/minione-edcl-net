describe('Login Page (Real API)', () => {
  it('should attempt login using the real local backend', () => {
    cy.visit('/login');

    // Fill the form with real credentials
    // Note: The backend must be running for this test to pass
    cy.get('input[formControlName="email"]').type('admin@edcl.com');
    cy.get('input[formControlName="password"]').type('password123');
    
    // Spy on the real API request
    cy.intercept('POST', '**/api/v1/web/auth/users/login').as('realLoginRequest');

    cy.get('button[type="submit"]').click();

    // Wait for the real API request to complete and check its status code
    cy.wait('@realLoginRequest').then((interception) => {
      if (interception.response && interception.response.statusCode !== 200) {
        // The real backend returned an error (e.g., 400 Bad Request)
        cy.log('Real API returned an error for these credentials. Checking error alert...');
        cy.get('.alert-error').should('be.visible');
      } else {
        // The real backend returned 200 OK
        cy.log('Real API returned success. Checking redirection...');
        cy.url().should('not.include', '/login');
      }
    });
  });
});
