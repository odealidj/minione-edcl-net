describe('Login Page (Mocked API)', () => {
  beforeEach(() => {
    // Intercept the API call so we don't rely on the local backend server
    cy.intercept('POST', '**/api/v1/web/auth/users/login', {
      statusCode: 200,
      body: {
        data: {
          accessToken: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJhZG1pbkBlZGNsLmNvbSIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IkFETUlOIn0K.fake-signature',
          refreshToken: 'mock-refresh-token',
          user: {
            id: 1,
            email: 'admin@edcl.com',
            role: 'ADMIN'
          }
        },
        message: 'Success',
        isSuccess: true
      }
    }).as('loginRequest');
  });

  it('should successfully log in and redirect to dashboard', () => {
    cy.visit('/login');

    // Fill the form
    cy.get('input[formControlName="email"]').type('admin@edcl.com');
    cy.get('input[formControlName="password"]').type('password123');
    
    // Submit
    cy.get('button[type="submit"]').click();

    // Wait for the mocked request to be called
    cy.wait('@loginRequest');

    // Assert that we are redirected to the dashboard (or home page)
    cy.url().should('not.include', '/login');
  });

  it('should display error message on failure', () => {
    // Override the intercept for failure case
    cy.intercept('POST', '**/api/v1/web/auth/users/login', {
      statusCode: 400,
      body: {
        message: 'Invalid credentials',
        isSuccess: false
      }
    }).as('loginRequestFail');

    cy.visit('/login');

    cy.get('input[formControlName="email"]').type('wrong@edcl.com');
    cy.get('input[formControlName="password"]').type('wrongpass');
    cy.get('button[type="submit"]').click();

    cy.wait('@loginRequestFail');

    // Check if error message is displayed in the alert
    cy.get('.alert-error').should('be.visible').and('contain', 'Invalid credentials');
  });
});
