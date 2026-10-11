CREATE USER lab_user WITH ENCRYPTED PASSWORD 'lab_pass';

-- payments
CREATE DATABASE lab_netmicro_payments OWNER lab_user;
GRANT ALL PRIVILEGES ON DATABASE lab_netmicro_payments TO lab_user;

--hangfire
CREATE DATABASE lab_netmicro_jobs OWNER lab_user;
GRANT ALL PRIVILEGES ON DATABASE lab_netmicro_jobs TO lab_user;

--policy
CREATE DATABASE lab_netmicro_policy OWNER lab_user;
GRANT ALL PRIVILEGES ON DATABASE lab_netmicro_policy TO lab_user;

--pricing
CREATE DATABASE lab_netmicro_pricing OWNER lab_user;
GRANT ALL PRIVILEGES ON DATABASE lab_netmicro_pricing TO lab_user;


