# Segurança

Para relatar uma vulnerabilidade, escreva para `security@raims.com` com uma
reprodução mínima e sem credenciais ou dados pessoais. Evite divulgar o detalhe
explorável antes da correção coordenada.

Use o SDK no servidor. Armazene chaves e segredos em um cofre e limite as
permissões da credencial à integração. Não inclua credenciais, corpos de
requisição, documentos pessoais ou URLs temporárias em logs e issues.

Mantenha o .NET, o sistema operacional e as dependências atualizados. Não
desative TLS ou a verificação de certificados. Um transporte próprio deve
preservar os mesmos limites de redirects e de envio de credenciais.

Valide a assinatura do webhook nos bytes originais. Confira ambiente e tipo do
evento, deduplique de forma durável e confirme o trabalho antes do aceite HTTP.

Após timeout ou perda de resposta de uma escrita, consulte o recurso e conserve
a identidade da intenção antes de decidir uma nova tentativa.
