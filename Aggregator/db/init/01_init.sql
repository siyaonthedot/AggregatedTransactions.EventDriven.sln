CREATE TABLE IF NOT EXISTS transactions (
    transaction_id VARCHAR(64) PRIMARY KEY,
    customer_id VARCHAR(64) NOT NULL,
    bank VARCHAR(64) NOT NULL,
    posted_at_utc TIMESTAMP NOT NULL,
    category VARCHAR(64) NOT NULL,
    amount NUMERIC(18,2) NOT NULL,
    currency VARCHAR(8) NOT NULL,
    description VARCHAR(256)
);

CREATE INDEX IF NOT EXISTS idx_transactions_customer_postedat
    ON transactions(customer_id, posted_at_utc);