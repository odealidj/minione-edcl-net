describe('Register Page (Real API)', () => {
  beforeEach(() => {
    cy.visit('/register');
  });

  it('should attempt registration using the real local backend', () => {
    cy.intercept('POST', '**/api/v1/web/auth/users/register').as('realRegisterRequest');

    // Use a unique email to avoid "already exists" errors if run multiple times
    const uniqueEmail = `testuser_${Date.now()}@example.com`;

    cy.get('input[formControlName="name"]').type('Real E2E User');
    cy.get('input[formControlName="email"]').type(uniqueEmail);
    cy.get('input[formControlName="password"]').type('password123');
    
    cy.get('button[type="submit"]').click();

    cy.wait('@realRegisterRequest').then((interception) => {
      if (interception.response && interception.response.statusCode !== 200) {
        cy.log('Real API returned an error for registration. Checking error alert...');
        cy.get('.alert-error').should('be.visible');
      } else {
        cy.log('Real API returned success. Checking redirection...');
        cy.url().should('include', '/login');
      }
    });
  });
});
