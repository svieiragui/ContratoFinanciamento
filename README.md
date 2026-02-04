# Contracts API - Sistema de Financiamento

## 📋 Descrição

A **Contracts API** é uma aplicação backend desenvolvida em **.NET 9** para gerenciar contratos de financiamento, com funcionalidades para:

- **Gestão de Contratos**: Criar, listar, buscar e cancelar contratos de financiamento
- **Gestão de Pagamentos**: Registrar e consultar pagamentos de contratos
- **Resumo de Clientes**: Obter informações consolidadas sobre contratos e pagamentos de um cliente

A aplicação utiliza uma arquitetura em camadas com padrões modernos como CQRS, Repository Pattern e Dependency Injection.

---

## 🏗️ Arquitetura da Aplicação

A solução segue uma arquitetura baseada em **Clean Architecture** com separação clara de responsabilidades:

### Estrutura de Projetos

```
src/
├── ContractsApi.Api              # Projeto Web API (Presentation Layer)
├── ContractsApi.Application      # Lógica de aplicação (CQRS Handlers)
├── ContractsApi.Domain           # Entidades e interfaces de domínio
└── ContractsApi.Infrastructure   # Acesso a dados (Repositories)

tests/
├── ContractsApi.UnitTests        # Testes unitários
└── ContractsApi.IntegrationTests # Testes de integração
```

### Camadas

#### 1. **Domain Layer** (`ContractsApi.Domain`)
- Contém as entidades de negócio e interfaces de repositório
- Define contratos sem dependências externas
- **Responsabilidade**: Modelar a lógica de negócio pura

#### 2. **Application Layer** (`ContractsApi.Application`)
- Implementa padrão **CQRS** (Command Query Responsibility Segregation)
- **Handlers**: Processam comandos e queries
- **Validators**: Validações de negócio usando FluentValidation
- **Responsabilidade**: Orquestrar operações de negócio

#### 3. **Infrastructure Layer** (`ContractsApi.Infrastructure`)
- Implementação de repositórios
- Acesso a dados usando **Dapper** e **PostgreSQL**
- **Responsabilidade**: Abstrair detalhes técnicos de acesso a dados

#### 4. **API Layer** (`ContractsApi.Api`)
- Controllers REST
- Configuração de DI (Dependency Injection)
- Autenticação JWT
- Documentação Swagger
- Health Checks
- Logging com Serilog
- **Responsabilidade**: Expor a aplicação via HTTP

### Tecnologias Principais

| Tecnologia | Versão | Propósito |
|------------|--------|----------|
| .NET | 9.0 | Framework base |
| PostgreSQL | Latest | Banco de dados |
| Dapper | 2.1.66 | ORM leve para dados |
| MediatR | 14.0.0 | Implementação CQRS |
| FluentValidation | 12.1.1 | Validações |
| Serilog | 10.0.0 | Logging |
| Swagger/Swashbuckle | 6.8.0 | Documentação API |
| JWT Bearer | 7.0.20 | Autenticação |
| xUnit | 2.9.2 | Framework de testes |
| Testcontainers | 4.10.0 | PostgreSQL em testes |

---

## 🚀 Como Rodar a Aplicação

### Pré-requisitos

- **.NET 9 SDK** ou superior
- **Docker** e **Docker Compose**
- **PostgreSQL** (opcional, pode ser via Docker)
- **Git**

### 1️ Clonar o Repositório

```bash
git clone https://github.com/svieiragui/ContratoFinanciamento.git
cd ContratoFinanciamento
```

### 2️ Configurar Banco de Dados com Docker Compose

A forma mais rápida é usar Docker Compose para subir o PostgreSQL:

```bash
docker-compose up -d
```

Isto irá:
- Criar um container PostgreSQL na porta `5432`
- Configurar o usuário `contracts_user` com senha `contracts_password`
- Criar o banco de dados `contracts_db`

### 3️ Restaurar Pacotes NuGet

```bash
dotnet restore
```

### 4️ Executar a Aplicação

```bash
cd src/ContractsApi.Api
dotnet run
```

A API estará disponível em: **https://localhost:7100** (ou http://localhost:5000)

### 5️ Acessar o Swagger

Abra seu navegador e acesse:

```
https://localhost:7100/
```

O Swagger UI estará disponível automaticamente na raiz da aplicação.

---

## 🧪 Como Testar a Aplicação

### Testes Unitários

Os testes unitários validam a lógica de negócio isoladamente usando **xUnit** e **Moq**.

#### Executar todos os testes unitários

```bash
dotnet test tests/ContractsApi.UnitTests
```
---

### Testes de Integração

Os testes de integração validam a aplicação completa usando **Testcontainers** para PostgreSQL.

#### Pré-requisitos para Testes de Integração

- **Docker** instalado e rodando
- A aplicação ainda não precisa estar em execução

#### Executar todos os testes de integração

```bash
dotnet test tests/ContractsApi.IntegrationTests
```


### 3Executar Todos os Testes

```bash
# Executar todos os testes (unit + integration)
dotnet test

# Com cobertura de código
dotnet test /p:CollectCoverageMetrics=true
```

---

## 🔐 Autenticação JWT

### Credenciais Padrão

As credenciais abaixo estão configuradas em `appsettings.json`:

```json
{
  "FixedUser": {
    "Username": "lorem",
    "Password": "ipsum"
  }
}
```

### Como Autenticar

#### 1. Fazer Login (Se houver endpoint de autenticação)

```bash
curl -X POST https://localhost:7100/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "lorem",
    "password": "ipsum"
  }'
```

Isso retornará um token JWT no formato:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

#### 2. Usar o Token em Requisições

No Swagger:
- Clique no botão **"Authorize"**
- Copie o token (sem "Bearer ")
- Cole no campo de autenticação

Via cURL:
```bash
curl -X GET https://localhost:7100/api/contratos \
  -H "Authorization: Bearer seu_token_aqui"
```

### Configurações JWT

As configurações JWT estão em `appsettings.json`:


### Visualizar Logs

```bash
# Logs em tempo real
tail -f logs/log-*.txt

# Ou abrir o arquivo direto
cat logs/log-2024-01-15.txt
```

---

### Testes de Integração falham

**Solução**: Certifique-se que Docker está rodando:

```bash
docker ps
```

---

## 📄 Licença

Este projeto é fornecido como está para fins educacionais.

---

## 👥 Autores

- [GitHub](https://github.com/svieiragui)
