# Relatório de Análise: Qualidade vs Características de Processo em Repositórios Java

Este relatório apresenta análises teóricas e a base para as repostas das Questões de Pesquisa (RQs) estabelecidas. Ele tem o objetivo de cruzar as métricas de processo de software (Popularidade, Tamanho, Atividade e Maturidade) com as métricas de qualidade de código (CBO, DIT, LCOM) obtidas através de ferramentas como o CK.

> **Importante:** Como os dados finais dependem da coleta executada no seu hardware para os 1.000 repositórios usando o `Program.cs`, você deve preencher os locais indicados com os coeficientes de correlação (Pearson/Spearman) gerados pela sua aplicação.

---

## RQ 01. Qual a relação entre a popularidade dos repositórios e as suas características de qualidade?
**Métrica de Processo:** Popularidade (Número de estrelas)  
**Métricas de Qualidade:** CBO, DIT, LCOM  

**Análise:**
Estudos em Engenharia de Software indicam que repositórios mais populares tendem a ter um número maior de contribuidores ativos e de revisões de código (*Code Review*). 
- **CBO (Acoplamento):** Pode se apresentar de forma moderada a alta devido à complexidade exigida na integração de múltiplas *features* propostas pela grande comunidade.
- **DIT (Herança):** Projetos abertos e populares costumam limitar árvores de herança profundas, preferindo Composição sobre Herança, para facilitar o entendimento de desenvolvedores externos. É esperada pouca correlação direta.
- **LCOM (Coesão):** Repositórios muito populares costumam manter padrões rígidos de qualidade visual, de modo que métodos e classes apresentem maior coesão (valores mais baixos de LCOM). No entanto, classes *core* frequentemente utilizadas podem inevitavelmente se tornar "inchadas".

**Conclusão dos Dados Coletados:**
*(Preencher com a correlação obtida nos arquivos da coleta)*

---

## RQ 02. Qual a relação entre a maturidade dos repositórios e as suas características de qualidade?
**Métrica de Processo:** Maturidade (Idade em anos)  
**Métricas de Qualidade:** CBO, DIT, LCOM  

**Análise:**
Sistemas mais maduros (mais antigos) frequentemente sofrem dos efeitos de *envelhecimento de software*, acumulando débito técnico durante a sua evolução ao longo dos anos.
- **CBO:** Tende a apresentar forte relação positiva com a maturidade. A adição contínua de funcionalidades sem tempo constante ou viabilidade de refatoração geral gera acoplamento excessivo com o passar das versões.
- **DIT:** A profundidade de herança costuma se estabelecer inicialmente e tende a se estabilizar. Projetos maduros em Java de 10 anos atrás podem ter DITs maiores do que projetos novos devido à mudança de padrões arquiteturais da época.
- **LCOM:** A falta de coesão tende a piorar sistematicamente com a idade. Métodos ganham condicionais extras e classes assumem escopos novos para os quais não foram originalmente projetadas, gerando *God Classes*.

**Conclusão dos Dados Coletados:**
*(Preencher com a correlação obtida nos arquivos da coleta)*

---

## RQ 03. Qual a relação entre a atividade dos repositórios e as suas características de qualidade?
**Métrica de Processo:** Atividade (Número de releases)  
**Métricas de Qualidade:** CBO, DIT, LCOM  

**Análise:**
Apesar da maturidade, repositórios extremamente ativos (ciclos curtos e constantes de *releases*) exigem geralmente uma esteira ágil robusta para suportar essa cadência de entrega.
- **CBO e LCOM:** Projetos com alto volume de entregas costumam evitar sistemas excessivamente acoplados, pois códigos altamente interdependentes paralisam/impedem publicações rápidas. Pode-se esperar uma busca contínua por coesão. Ao mesmo tempo, caso os *releases* não acompanhem refatorações, o código degrada com a correria, piorando as métricas.
- **DIT:** Herança tem baixa associação direta com cadência de lançamentos, sendo reflexo, na maior parte das vezes, estritamente de decisões de design.

**Conclusão dos Dados Coletados:**
*(Preencher com a correlação obtida nos arquivos da coleta)*

---

## RQ 04. Qual a relação entre o tamanho dos repositórios e as suas características de qualidade?
**Métrica de Processo:** Tamanho (Linhas de Código - LOC e Linhas de Comentários)  
**Métricas de Qualidade:** CBO, DIT, LCOM  

**Análise:**
O volume e a magnitude física de um projeto de software apresentam as evidências e correlações mais visíveis nos marcadores de qualidade.
- **CBO:** O aumento físico do LOC quase sempre implica em maiores classes com necessidade intrínseca de comunicação umas com as outras (consequentemente elevando o acoplamento/CBO).
- **LCOM:** De forma semelhante, quanto mais linhas uma classe possui, mais provável de acumular uma diversidade maior de métodos e variáveis fracamente relacionadas, deteriorando a coesão de maneira drástica.
- **DIT:** Sistemas com milhares de linhas podem utilizar mais recursos de herança para reuso de base e polimorfismo complexo (por exemplo, na adoção pesada das APIs de *Spring Framework* ou interfaces Java nativas).
- **Comentários:** Tende a crescer naturalmente o LOC, mas áreas com elevadas quebras em métricas (arquivos com péssimos CBO ou LCOM) tendem a apresentar volumes desproporcionalmente maiores de anotações por parte dos desenvolvedores, que tentam explicar a lógica falha ou complexa.

**Conclusão dos Dados Coletados:**
*(Preencher com a correlação obtida de LOC nas classes vs. CK nos arquivos de coleta)*
