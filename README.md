# DiabetesBot

Projeto de TCC (Sistemas de Informação) de um bot para WhatsApp focado no auxílio à contagem de carboidratos e cálculo de dose de insulina bolus para pessoas com diabetes.

A ideia do projeto é permitir que o paciente envie o que vai comer por mensagem de texto normal (por exemplo, *"2 fatias de pão com ovo e um copo de leite"*) e receba o cálculo estimado de carboidratos da refeição junto com a sugestão de unidades de insulina, com base nos parâmetros passados pelo médico dele (RIC, Fator de Sensibilidade e Glicemia Alvo).

## Tecnologias

- C# / .NET 10 (ASP.NET Core Web API)
- Entity Framework Core + PostgreSQL
- Docker e Docker Compose
- Evolution API (integração com WhatsApp)
- Google Gemini API (interpretação das mensagens e medidas caseiras)
- Tabelas nutricionais: TACO (banco local), Open Food Facts e USDA API
- xUnit e Moq para testes unitários

## Como rodar localmente

### Pré-requisitos
- .NET 10 SDK
- Docker Desktop

### Passo a passo

1. Clone o repositório e crie o arquivo `.env` na raiz a partir do `.env.example`:
```bash
cp .env.example .env
```

2. Preencha no `.env` as credenciais do banco e as chaves das APIs (Gemini, USDA e Evolution API).

3. Suba o banco de dados PostgreSQL e a Evolution API via Docker:
```bash
docker compose up -d
```

4. Entre na pasta da API e rode o projeto:
```bash
cd api
dotnet run
```
Ao iniciar pela primeira vez, a API já roda as migrations e faz o seed automático da tabela TACO no banco.

## Testes

Para rodar os testes unitários:
```bash
dotnet test
```
