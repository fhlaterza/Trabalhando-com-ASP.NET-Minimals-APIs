using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using minimal_api;
using minimal_api.Dominio.DTOs;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Enums;
using minimal_api.Dominio.Interfaces;
using minimal_api.Dominio.ModelViews;
using minimal_api.Dominio.Servicos;
using minimal_api.Infraestrutura.Db;

public class Startup
{
    public Startup(IConfiguration configuration)
    {

            Configuration = configuration;
            key = Configuration?.GetSection("Jwt")?.ToString() ?? "";
    }
    private string key = "";
    public IConfiguration Configuration { get; set;} = default!;

    public void ConfigureServices(IServiceCollection services)
    {
        // Services 
        services.AddAuthentication(option => 
        {
            option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(option => 
            {
                option.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

        services.AddAuthorization();

        services.AddScoped<IAdministradorServico, AdministradorServico>();
        services.AddScoped<IVeiculoServico, VeiculoServico>();

        //Adicionando Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options => 
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "insira o Token JWT aqui"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference= new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string [] {}
                }
            });
        });
        //

        services.AddDbContext<DbContexto>(options => 
            {
                options.UseMySql(
                    Configuration.GetConnectionString("MySql"),
                    ServerVersion.AutoDetect(Configuration.GetConnectionString("MySql"))
                );
            }

        );
        services.AddCors(options => 
        {
            options.AddDefaultPolicy
            (
                builder =>
                {
                    builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                }
            );
        });
    }
    
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseCors();

        app.UseEndpoints(endpoints => 
        {
            #region Home
            endpoints.MapGet("/", () => Results.Json(new Home())).AllowAnonymous().WithTags("Home");
            #endregion

            #region Administradores
            string GerarTokenJwt(Administrador administrador)
            {
                if(string.IsNullOrEmpty(key)) return string.Empty;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>()
                {
                    new Claim("Email", administrador.Email),
                    new Claim("Perfil", administrador.Perfil),
                    new Claim(ClaimTypes.Role, administrador.Perfil)
                };

                var token = new JwtSecurityToken
                (
                    claims: claims,
                    expires : DateTime.Now.AddDays(1),
                    signingCredentials: credentials
                );
                return new JwtSecurityTokenHandler().WriteToken(token);
            }

            endpoints.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) =>
            {
                //if(administradorServico.Login(loginDTO) != null )
                var adm = administradorServico.Login(loginDTO);
                if(adm !=null)
                {
                    string token = GerarTokenJwt(adm);
                    return Results.Ok(new AdministradorLogado
                    {
                        Email = adm.Email,
                        Perfil = adm.Perfil,
                        Token = token
                    });
                }
                else 
                    return Results.Unauthorized();
            })
            .AllowAnonymous()
            .RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm"})
            .WithTags("Administradores");

            endpoints.MapPost("/administradores", ([FromBody] AdministradorDTO administradorDTO, IAdministradorServico administradorServico) =>
            {
                var validacao = new ErrosDeValidacao{
                    Mensagens = new List<string>()
                };

                if(string.IsNullOrEmpty(administradorDTO.Email))
                    validacao.Mensagens.Add("Email não pode ser vazio");
                if(string.IsNullOrEmpty(administradorDTO.Senha))
                    validacao.Mensagens.Add("Senha não pode ser vazia");
                if(administradorDTO.Perfil == null)
                    validacao.Mensagens.Add("Perfil não pode ser vazio");

                if(validacao.Mensagens.Count >0)
                    return Results.BadRequest(validacao);

                var administrador = new Administrador
                {
                    Email = administradorDTO.Email,
                    Senha = administradorDTO.Senha,
                    Perfil = administradorDTO.Perfil?.ToString() ?? Perfil.Editor.ToString()
                };
                administradorServico.Incluir(administrador);
                return Results.Created($"/admnistradores/{administrador.Id}", new AdministradorModelView 
                    {
                        Id = administrador.Id,
                        Email = administrador.Email,
                        Perfil = administrador.Perfil
                    });
            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm"})
            .WithTags("Administradores");

            endpoints.MapGet("/Administradores", ([FromQuery] int? pagina, IAdministradorServico administradorServico ) =>
            {
                var adms = new List<AdministradorModelView>();
                var administradores = administradorServico.Todos(pagina);
                foreach (var adm in administradores) 
                {
                    adms.Add(new AdministradorModelView 
                    {
                        Id = adm.Id,
                        Email = adm.Email,
                        Perfil = adm.Perfil
                    });
                }

                return Results.Ok(adms);
            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm"})
            .WithTags("Administradores");

            endpoints.MapGet("/Administradores/{id}", ([FromRoute] int id, IAdministradorServico administradorServico ) =>
            {
                var administrador = administradorServico.BuscaPorId(id);
                if(administrador == null) return Results.NotFound();
                
                
                
                return Results.Ok(new AdministradorModelView 
                    {
                        Id = administrador.Id,
                        Email = administrador.Email,
                        Perfil = administrador.Perfil
                    });
            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm"})
            .WithTags("Administradores");
            #endregion

            #region Veiculos

            ErrosDeValidacao validaDTO(VeiculoDTO veiculoDTO)
            {
                var validacao = new ErrosDeValidacao 
                {
                    Mensagens = new List<string>()
                };

                if(string.IsNullOrEmpty(veiculoDTO.Nome))
                    validacao.Mensagens.Add("O nome não pode ser vazio");

                if(string.IsNullOrEmpty(veiculoDTO.Marca))
                    validacao.Mensagens.Add("A Marca não pode estar em Branco");

                if(veiculoDTO.Ano < 1950)
                    validacao.Mensagens.Add("Ano inválido ou inferior à 1950 não é aceito");
                return validacao;
            }
            ;

            endpoints.MapPost("/veiculos", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
            {
                
                var validacao = validaDTO(veiculoDTO);

                if(validacao.Mensagens.Count >0)
                    return Results.BadRequest(validacao);

                var veiculo = new Veiculo
                {
                    Nome = veiculoDTO.Nome,
                    Marca = veiculoDTO.Marca,
                    Ano = veiculoDTO.Ano 
                };
                veiculoServico.Incluir(veiculo);
                return Results.Created($"/veiculo/{veiculo.Id}", veiculo);
            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute{ Roles = "Editor, Adm"})
            .WithTags("Veiculos");

            endpoints.MapGet("/veiculos", ([FromQuery] int? pagina, IVeiculoServico veiculoServico ) =>
            {
                var veiculos = veiculoServico.Todos(pagina);
                return Results.Ok(veiculos);
            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute{ Roles = "Editor, Adm"})
            .WithTags("Veiculos");

            endpoints.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico ) =>
            {
                var veiculo = veiculoServico.BuscaPorId(id);
                if(veiculo == null) return Results.NotFound();
                return Results.Ok(veiculo);
            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute{ Roles = "Editor, Adm"})
            .WithTags("Veiculos");

            endpoints.MapPut("/veiculos/{id}", ([FromRoute] int id, VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico ) =>
            {
                var veiculo = veiculoServico.BuscaPorId(id);
                if(veiculo == null) return Results.NotFound();
                
                var validacao = validaDTO(veiculoDTO);

                if(validacao.Mensagens.Count >0)
                    return Results.BadRequest(validacao);
                

                veiculo.Nome = veiculoDTO.Nome;
                veiculo.Marca = veiculoDTO.Marca;
                veiculo.Ano = veiculoDTO.Ano;

                veiculoServico.Alterar(veiculo);
                return Results.Ok(veiculo);
            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm"})
            .WithTags("Veiculos");

            endpoints.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico ) =>
            {
                var veiculo = veiculoServico.BuscaPorId(id);
                if(veiculo == null) return Results.NotFound();

                veiculoServico.Apagar(veiculo);

                return Results.NoContent();
            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm"})
            .WithTags("Veiculos");
            #endregion
        });
    }
}
