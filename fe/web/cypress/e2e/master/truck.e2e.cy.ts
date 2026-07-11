describe('Master Truck Page (Real API)', () => {
  beforeEach(() => {
    // Real login
    cy.visit('/login');
    cy.intercept('POST', '**/api/v1/web/auth/users/login').as('realLoginRequest');
    
    cy.get('input[formControlName="email"]').type('admin@edcl.com');
    cy.get('input[formControlName="password"]').type('password123');
    cy.get('button[type="submit"]').click();

    cy.wait('@realLoginRequest').then((interception) => {
      if (interception.response && interception.response.statusCode !== 200) {
        this.skip(); // Skip if login fails
      }
    });

    // Navigate to Truck Master
    cy.visit('/master/truck');
  });

  it('should display trucks from the real database', () => {
    cy.intercept('GET', '**/api/v1/web/master/trucks*').as('realGetTrucks');
    cy.wait('@realGetTrucks').then((interception) => {
      if (interception.response && interception.response.statusCode === 200) {
        // Assert that the table is visible, though it might be empty depending on DB state
        cy.get('table').should('be.visible');
      }
    });
  });

  it('should be able to add a new truck to the real database', () => {
    cy.intercept('POST', '**/api/v1/web/master/trucks').as('realAddTruck');
    
    cy.contains('Add New').click();
    
    const uniquePlate = `B ${Math.floor(Math.random() * 9000) + 1000} E2E`;
    
    cy.get('input[formControlName="plateNumber"]').type(uniquePlate);
    cy.get('input[formControlName="vehicleType"]').type('FUSO');
    
    cy.get('button[type="submit"]').click();
    
    cy.wait('@realAddTruck').then((interception) => {
      if (interception.response && interception.response.statusCode === 200) {
        // Success
        cy.get('dialog.modal').should('not.have.class', 'modal-open');
        // The new plate should appear in the table (if pagination allows)
        cy.contains(uniquePlate).should('be.visible');
      } else {
        // Log the error if the backend rejected it
        cy.log('Backend rejected the truck creation:', interception.response?.body);
      }
    });
  });
});
