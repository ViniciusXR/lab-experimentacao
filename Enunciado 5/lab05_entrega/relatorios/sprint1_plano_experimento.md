# Lab05S01 - Desenho do Experimento

## Tema

Comparacao quantitativa entre consultas REST e GraphQL em um experimento controlado e reprodutivel localmente.

## Perguntas de pesquisa

- **RQ1:** Respostas as consultas GraphQL sao mais rapidas que respostas as consultas REST?
- **RQ2:** Respostas as consultas GraphQL tem tamanho menor que respostas as consultas REST?

## Hipoteses

### RQ1 - Tempo de resposta

- **H0-1:** nao ha diferenca estatisticamente significativa entre o tempo de resposta de REST e GraphQL.
- **H1-1:** o tempo de resposta de GraphQL e menor que o tempo de resposta de REST.

### RQ2 - Tamanho da resposta

- **H0-2:** nao ha diferenca estatisticamente significativa entre o tamanho das respostas REST e GraphQL.
- **H1-2:** o tamanho das respostas GraphQL e menor que o tamanho das respostas REST.

## Variaveis

### Variaveis independentes

- **Estilo de API:** REST ou GraphQL.
- **Cenario de consulta:** cinco consultas equivalentes sobre o mesmo dominio de dados.

### Variaveis dependentes

- **Tempo de resposta (ms):** tempo para montar, serializar e consumir a resposta JSON.
- **Tamanho da resposta (bytes):** quantidade de bytes UTF-8 retornados em cada trial.

## Tratamentos

- **REST:** endpoints com recursos completos e campos extras, simulando over-fetching comum em APIs REST.
- **GraphQL:** consulta com projecao explicita de campos, retornando apenas os dados solicitados pelo cliente.

## Objetos experimentais

O experimento usa um catalogo local sintetico e deterministico de repositorios, issues, releases e contribuidores. O mesmo snapshot alimenta os dois tratamentos, evitando variacao de rede, autenticacao, cache de provedores externos e mudancas em APIs publicas.

## Tipo de projeto experimental

Projeto intra-sujeitos (within-subject): cada cenario de consulta e executado nos dois tratamentos, REST e GraphQL. A ordem dos trials e embaralhada com semente fixa para reduzir vies de ordem.

## Quantidade de medicoes

- 5 cenarios de consulta.
- 2 tratamentos.
- 120 trials por tratamento em cada cenario.
- Total planejado: 1.200 medicoes.
- Rodadas de aquecimento: 20 execucoes por cenario/tratamento antes da coleta.

## Cenarios de consulta

| ID | Nome | Objetivo logico | Diferenca esperada |
|---|------|------------------|--------------------|
| Q1 | Lista de repositorios | Listar repositorios com metadados principais | REST retorna campos extras; GraphQL projeta somente campos de listagem |
| Q2 | Detalhe do repositorio | Consultar um repositorio especifico | REST retorna objeto completo; GraphQL retorna campos selecionados |
| Q3 | Issues recentes | Consultar issues e autores | REST retorna corpo, timeline e metadados; GraphQL retorna dados necessarios |
| Q4 | Contribuidores | Consultar contribuidores e contribuicoes | REST retorna perfis completos; GraphQL retorna resumo |
| Q5 | Visao aninhada | Obter repositorio, issues, contribuidores e release | REST soma multiplos endpoints; GraphQL retorna grafo unico |

## Analise estatistica planejada

- Estatisticas descritivas: media, mediana, desvio padrao, minimo, maximo e p95.
- Teste de Mann-Whitney U para comparacao entre tratamentos, adotando alfa = 0,05.
- Tamanho de efeito por Cliff's delta.
- Resposta as RQs baseada em significancia estatistica, direcao da mediana e magnitude pratica da diferenca.

## Ameacas a validade

- **Validade interna:** medicao local reduz ruido de rede, mas tambem remove latencia real de servidores externos. Mitigacao: medir serializacao, montagem de resposta e consumo JSON, que sao etapas comuns aos dois estilos.
- **Validade externa:** catalogo sintetico pode nao representar todas as APIs reais. Mitigacao: usar cenarios comuns em APIs de repositorios, com objetos aninhados, listas e detalhes.
- **Validade de construto:** GraphQL foi modelado como projecao de campos, nao como servidor GraphQL completo com parser e resolvedores complexos. Mitigacao: explicitar a decisao e comparar o beneficio de reducao de payload, principal diferenca observavel no laboratorio.
- **Validade de conclusao:** tempos muito pequenos podem gerar ruido. Mitigacao: aquecimento, repeticoes, ordem embaralhada, uso de mediana e teste nao parametrico.