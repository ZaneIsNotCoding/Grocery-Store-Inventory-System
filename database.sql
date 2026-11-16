CREATE DATABASE IF NOT EXISTS grocery_inventory CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE grocery_inventory;

CREATE TABLE IF NOT EXISTS app_users (
  user_id INT AUTO_INCREMENT PRIMARY KEY,
  username VARCHAR(60) NOT NULL UNIQUE,
  password_hash VARCHAR(256) NOT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS products (
  product_id INT AUTO_INCREMENT PRIMARY KEY,
  sku VARCHAR(50) NOT NULL UNIQUE,
  product_name VARCHAR(150) NOT NULL,
  category VARCHAR(100) NOT NULL DEFAULT '',
  unit VARCHAR(30) NOT NULL DEFAULT 'piece',
  unit_price DECIMAL(12,2) NOT NULL DEFAULT 0,
  quantity INT NOT NULL DEFAULT 0,
  reorder_level INT NOT NULL DEFAULT 5,
  is_active BOOLEAN NOT NULL DEFAULT TRUE,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CHECK (unit_price >= 0), CHECK (quantity >= 0), CHECK (reorder_level >= 0),
  INDEX idx_product_name (product_name), INDEX idx_product_active (is_active)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS stock_transactions (
  transaction_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  product_id INT NOT NULL,
  transaction_type ENUM('IN','OUT') NOT NULL,
  quantity INT NOT NULL,
  note VARCHAR(255) NOT NULL DEFAULT '',
  user_id INT NOT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CHECK (quantity > 0),
  CONSTRAINT fk_stock_product FOREIGN KEY (product_id) REFERENCES products(product_id),
  CONSTRAINT fk_stock_user FOREIGN KEY (user_id) REFERENCES app_users(user_id),
  INDEX idx_stock_created (created_at), INDEX idx_stock_product (product_id)
) ENGINE=InnoDB;
