using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace minimal_api.Dominio.ModelViews
{
    public struct Home
    {
        public string Mensagem { get => "Bem Vindo a API de Veículos - MinimalAPI";}
        public string Doc { get => "/swagger";}
    }
}