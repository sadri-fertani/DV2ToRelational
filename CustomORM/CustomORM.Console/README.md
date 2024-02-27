# Introduction

# Transformation
Il faut Transformer les entites (seulement les Hub) par EF vers des Dto
Pour ce besoin, il faut utiliser CustomORM.Converter

# Prerequis
Use EF Core Power tools to generate Entities
Use option :Use attributs DataAnnotations

# Condition de nommage
HUB 
- Table : h_{client}
- Entity : H{Client}

SATELLITE
- Table : s_{client}_{adresse}
- Entity : S{Client}{Adresse}


PIT
- Table : p_{client}
- Entity : P{Client}

VIEW
- View : v_{client}
- Entity : V{Client}

no restriction for the name for hashkey