// Initialize MongoDB with default data
db = db.getSiblingDB('rdp_relay');

// Create collections
db.createCollection('tenants');
db.createCollection('users');
db.createCollection('agents');
db.createCollection('sessions');

// Create indexes
db.tenants.createIndex({ "domain": 1 }, { unique: true });
db.tenants.createIndex({ "isActive": 1 });

db.users.createIndex({ "email": 1 }, { unique: true });
db.users.createIndex({ "tenantId": 1 });
db.users.createIndex({ "isActive": 1 });

db.agents.createIndex({ "tenantId": 1 });
db.agents.createIndex({ "machineName": 1, "tenantId": 1 });
db.agents.createIndex({ "status": 1 });
db.agents.createIndex({ "lastHeartbeat": 1 });

db.sessions.createIndex({ "tenantId": 1 });
db.sessions.createIndex({ "agentId": 1 });
db.sessions.createIndex({ "userId": 1 });
db.sessions.createIndex({ "status": 1 });
db.sessions.createIndex({ "createdAt": 1 });
db.sessions.createIndex({ "connectCode": 1 }, { unique: true, sparse: true });

// Insert default tenant
db.tenants.insertOne({
  _id: ObjectId(),
  name: "Default Tenant",
  domain: "default.local",
  isActive: true,
  createdAt: new Date(),
  settings: {
    maxAgents: 100,
    maxSessions: 50,
    sessionTimeoutMinutes: 480,
    allowedIpRanges: ["0.0.0.0/0"]
  }
});

// Get the default tenant ID
const defaultTenant = db.tenants.findOne({ domain: "default.local" });

// Insert default admin user (password is 'admin123' hashed with bcrypt)
db.users.insertOne({
  _id: ObjectId(),
  email: "admin@rdprelay.local",
  firstName: "System",
  lastName: "Administrator",
  passwordHash: "$2a$10$K8qvVCVjJbDGNfpjWQQRJeWnCQdGKUJJBxVZQFHPKjL2YQJ5YjKqG",
  role: "SystemAdmin",
  tenantId: defaultTenant._id,
  isActive: true,
  createdAt: new Date(),
  lastLoginAt: null
});

print("Database initialized successfully with default tenant and admin user");
print("Default admin credentials: admin@rdprelay.local / admin123");
