describe('Register Page (Mocked API)', () => {
  beforeEach(() => {
    cy.visit('/register');
  });

  it('should display validation errors if form is submitted empty', () => {
    cy.get('button[type="submit"]').click();
    
    // Check validation messages
    cy.contains('Nama lengkap wajib diisi').should('be.visible');
    cy.contains('Email wajib diisi').should('be.visible');
    cy.contains('Password wajib diisi').should('be.visible');
  });

  it('should successfully register and redirect to login', () => {
    // Intercept the API call and return success
    cy.intercept('POST', '**/api/v1/web/auth/users/register', {
      statusCode: 200,
      body: {
        data: {
          id: 2,
          name: 'Test User',
          email: 'test@example.com'
        },
        message: 'Success',
        isSuccess: true
      }
    }).as('registerRequest');

    cy.get('input[formControlName="name"]').type('Test User');
    cy.get('input[formControlName="email"]').type('test@example.com');
    cy.get('input[formControlName="password"]').type('password123');
    
    cy.get('button[type="submit"]').click();

    // Verify API is called
    cy.wait('@registerRequest');

    // Should redirect to login
    cy.url().should('include', '/login');
  });

  it('should display error message on registration failure (e.g. Email used)', () => {
    // Intercept the API call and return 400 Bad Request
    cy.intercept('POST', '**/api/v1/web/auth/users/register', {
      statusCode: 400,
      body: {
        message: 'Email already in use',
        isSuccess: false
      }
    }).as('registerFailureRequest');

    cy.get('input[formControlName="name"]').type('Duplicate User');
    cy.get('input[formControlName="email"]').type('admin@edcl.com');
    cy.get('input[formControlName="password"]').type('password123');
    
    cy.get('button[type="submit"]').click();

    cy.wait('@registerFailureRequest');

    // Should show error alert
    cy.get('.alert-error').should('be.visible');
    cy.get('.alert-error').should('contain.text', 'Email already in use');
  });
});
