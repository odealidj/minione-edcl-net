describe('Master Truck Page (Mocked API)', () => {
  const fakeJwt = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJhZG1pbkBlZGNsLmNvbSIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IkFETUlOIn0K.fake-signature';

  beforeEach(() => {
    window.localStorage.setItem('access_token', fakeJwt);

    // Intercept GET trucks list
    cy.intercept('GET', '**/api/v1/web/master/trucks*', {
      statusCode: 200,
      body: {
        data: [
          { id: 1, plateNumber: 'B 1234 CD', vehicleType: 'FUSO' },
          { id: 2, plateNumber: 'D 5678 EF', vehicleType: 'CDE' }
        ],
        pagination: {
          page: 1,
          page_size: 10,
          total_items: 2,
          total_pages: 1,
          has_next: false,
          has_previous: false
        },
        message: 'Success',
        status: 'success',
        isSuccess: true
      }
    }).as('getTrucks');

    cy.visit('/master/truck');
  });

  it('should display the list of trucks from mock API', () => {
    cy.wait('@getTrucks');

    // Should render 2 rows in the table
    cy.get('tbody tr').should('have.length', 2);
    cy.contains('B 1234 CD').should('be.visible');
    cy.contains('D 5678 EF').should('be.visible');
  });

  it('should open the add new truck modal', () => {
    cy.wait('@getTrucks');
    
    // Click Add New button
    cy.contains('Add New').click();

    // Verify modal is open using the native HTML dialog property or visibility
    cy.get('dialog.modal').should('have.prop', 'open', true);
    cy.contains('Add Truck').should('be.visible');
  });

  it('should successfully submit a new truck', () => {
    cy.wait('@getTrucks');
    
    // Intercept POST request for new truck
    cy.intercept('POST', '**/api/v1/web/master/trucks', {
      statusCode: 200,
      body: {
        data: 3, // new ID
        message: 'Successfully created',
        status: 'success',
        isSuccess: true
      }
    }).as('addTruck');

    cy.contains('Add New').click();
    
    cy.get('input[formControlName="plateNumber"]').type('L 9999 AA');
    cy.get('input[formControlName="vehicleType"]').type('WINGBOX');
    
    cy.get('button[type="submit"]').click();
    
    cy.wait('@addTruck');
    
    // Modal should close
    cy.get('dialog.modal').should('not.have.prop', 'open', true);
  });
});
