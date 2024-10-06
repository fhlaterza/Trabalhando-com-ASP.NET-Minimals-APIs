using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Interfaces;

namespace Test.Mocks
{
    public class VeiculoServicoMock
    {
        public class AdministradorServicoMock : IVeiculoServico
        {
            private static List<Veiculo> veiculos = new List<Veiculo>();
            public void Alterar(Veiculo veiculo)
            {
                veiculo.Id = veiculos.Count() +1;
                
            }

            public void Apagar(Veiculo veiculo)
            {
                veiculo.Id = veiculos.Count() +1;
                veiculos.Remove(veiculo);
            }

            public Veiculo? BuscaPorId(int id)
            {
                return veiculos.Find( v => v.Id== id);
            }

            public void Incluir(Veiculo veiculo)
            {
                veiculo.Id = veiculos.Count() +1;
                veiculos.Add(veiculo);
            
                //return veiculo;
            }

            public List<Veiculo> Todos(int? pagina = 1, string? nome = null, string? marca = null)
            {
                return veiculos;
            }
        }
    }
}