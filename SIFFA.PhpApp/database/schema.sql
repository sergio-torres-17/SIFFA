-- SIFFA · esquema MySQL 5.7+ / MariaDB 10.3+
-- Equivalente al proyecto SIFFA.Database (SQL Server). Los identificadores son UUID en CHAR(36)
-- generados por la aplicación. Se puede ejecutar varias veces: solo crea lo que falta.

CREATE TABLE IF NOT EXISTS users (
  user_id      CHAR(36)     NOT NULL,
  first_name   VARCHAR(100) NOT NULL,
  last_name    VARCHAR(100) NOT NULL,
  email        VARCHAR(100) NOT NULL,
  created_at   DATETIME     NOT NULL,
  PRIMARY KEY (user_id),
  UNIQUE KEY ux_users_email (email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS credentials (
  credential_id CHAR(36)     NOT NULL,
  user_id       CHAR(36)     NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  created_at    DATETIME     NOT NULL,
  PRIMARY KEY (credential_id),
  KEY ix_credentials_user (user_id),
  CONSTRAINT fk_credentials_user FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Intentos fallidos de inicio de sesión (limita la fuerza bruta).
CREATE TABLE IF NOT EXISTS login_attempts (
  id           BIGINT       NOT NULL AUTO_INCREMENT,
  ip           VARCHAR(45)  NOT NULL,
  email        VARCHAR(100) NOT NULL,
  attempted_at DATETIME     NOT NULL,
  PRIMARY KEY (id),
  KEY ix_login_attempts_ip (ip, attempted_at),
  KEY ix_login_attempts_email (email, attempted_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS areas (
  area_id        CHAR(36)     NOT NULL,
  description    VARCHAR(100) NOT NULL,
  insertion_date DATETIME     NOT NULL,
  PRIMARY KEY (area_id),
  UNIQUE KEY ux_areas_description (description)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS employee_types (
  employee_type_id CHAR(36)     NOT NULL,
  name             VARCHAR(50)  NOT NULL,
  description      VARCHAR(200) NULL,
  insertion_date   DATETIME     NOT NULL,
  PRIMARY KEY (employee_type_id),
  UNIQUE KEY ux_employee_types_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS add_types (
  add_type_id    CHAR(36)     NOT NULL,
  name           VARCHAR(50)  NOT NULL,
  description    VARCHAR(200) NULL,
  insertion_date DATETIME     NOT NULL,
  PRIMARY KEY (add_type_id),
  UNIQUE KEY ux_add_types_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS access_actions (
  access_action_id CHAR(36)     NOT NULL,
  name             VARCHAR(50)  NOT NULL,
  description      VARCHAR(200) NULL,
  insertion_date   DATETIME     NOT NULL,
  PRIMARY KEY (access_action_id),
  UNIQUE KEY ux_access_actions_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS employees (
  employee_id         CHAR(36)     NOT NULL,
  employee_type_id    CHAR(36)     NOT NULL,
  internal_identifier VARCHAR(100) NOT NULL,
  area_id             CHAR(36)     NOT NULL,
  first_name          VARCHAR(100) NOT NULL,
  last_name           VARCHAR(100) NULL,
  email               VARCHAR(100) NOT NULL,
  insertion_date      DATETIME     NOT NULL,
  PRIMARY KEY (employee_id),
  UNIQUE KEY ux_employees_identifier (internal_identifier),
  UNIQUE KEY ux_employees_email (email),
  KEY ix_employees_area (area_id),
  KEY ix_employees_type (employee_type_id),
  CONSTRAINT fk_employees_area FOREIGN KEY (area_id) REFERENCES areas (area_id),
  CONSTRAINT fk_employees_type FOREIGN KEY (employee_type_id) REFERENCES employee_types (employee_type_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Adicionales que recibe cada empleado de un rubro. Solo uno de area_id / employee_type_id viene lleno.
CREATE TABLE IF NOT EXISTS add_type_employees (
  add_type_employee_id CHAR(36) NOT NULL,
  add_type_id          CHAR(36) NOT NULL,
  employee_type_id     CHAR(36) NULL,
  area_id              CHAR(36) NULL,
  quantity             INT      NOT NULL DEFAULT 0,
  insertion_date       DATETIME NOT NULL,
  PRIMARY KEY (add_type_employee_id),
  UNIQUE KEY ux_add_type_employees (employee_type_id, add_type_id, area_id),
  KEY ix_ate_add_type (add_type_id),
  KEY ix_ate_area (area_id),
  CONSTRAINT fk_ate_add_type FOREIGN KEY (add_type_id) REFERENCES add_types (add_type_id),
  CONSTRAINT fk_ate_area FOREIGN KEY (area_id) REFERENCES areas (area_id),
  CONSTRAINT fk_ate_type FOREIGN KEY (employee_type_id) REFERENCES employee_types (employee_type_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Cada fila es un boleto.
CREATE TABLE IF NOT EXISTS add_employees (
  add_employee_id CHAR(36)   NOT NULL,
  add_type_id     CHAR(36)   NOT NULL,
  employee_id     CHAR(36)   NOT NULL,
  is_delivered    TINYINT(1) NOT NULL DEFAULT 0,
  delivery_date   DATETIME   NULL,
  insertion_date  DATETIME   NOT NULL,
  PRIMARY KEY (add_employee_id),
  KEY ix_ae_employee_type (employee_id, add_type_id),
  KEY ix_ae_add_type (add_type_id),
  CONSTRAINT fk_ae_add_type FOREIGN KEY (add_type_id) REFERENCES add_types (add_type_id),
  CONSTRAINT fk_ae_employee FOREIGN KEY (employee_id) REFERENCES employees (employee_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS traces (
  trace_id         CHAR(36)     NOT NULL,
  access_action_id CHAR(36)     NOT NULL,
  employee_id      CHAR(36)     NOT NULL,
  trace_date       DATETIME     NOT NULL,
  description      VARCHAR(100) NOT NULL,
  insertion_date   DATETIME     NOT NULL,
  PRIMARY KEY (trace_id),
  KEY ix_traces_employee_date (employee_id, trace_date),
  KEY ix_traces_action (access_action_id),
  CONSTRAINT fk_traces_action FOREIGN KEY (access_action_id) REFERENCES access_actions (access_action_id),
  CONSTRAINT fk_traces_employee FOREIGN KEY (employee_id) REFERENCES employees (employee_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
