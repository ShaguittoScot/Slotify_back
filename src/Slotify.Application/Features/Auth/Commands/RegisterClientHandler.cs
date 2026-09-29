namespace Slotify.Application.Features.Auth.Commands;

using MediatR;
using Slotify.Application.Common.Interfaces;
using Slotify.Application.Common.Models;
using Slotify.Application.Features.Auth.DTOs;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;

public class RegisterClientHandler(
    IClientRepository clientRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<RegisterClientCommand, Result<ClientDto>>
{
    private readonly IClientRepository _clientRepository = clientRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<ClientDto>> Handle(
        RegisterClientCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Verificar si ya existe por SupabaseId o Email
        var existingBySupabase = await _clientRepository.GetBySupabaseIdAsync(request.Id, cancellationToken);
        if (existingBySupabase != null)
        {
            return Result<ClientDto>.Ok(new ClientDto
            {
                Id = existingBySupabase.Id,
                SupabaseId = existingBySupabase.SupabaseId,
                FirstName = existingBySupabase.FirstName,
                LastName = existingBySupabase.LastName,
                FullName = existingBySupabase.FullName,
                Email = existingBySupabase.Email,
                Phone = existingBySupabase.Phone
            });
        }

        if (await _clientRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            // Si el correo ya existe, podemos actualizar su SupabaseId
            var existingByEmail = await _clientRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (existingByEmail != null && existingByEmail.SupabaseId == null)
            {
                existingByEmail.SupabaseId = request.Id;
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<ClientDto>.Ok(new ClientDto
                {
                    Id = existingByEmail.Id,
                    SupabaseId = existingByEmail.SupabaseId,
                    FirstName = existingByEmail.FirstName,
                    LastName = existingByEmail.LastName,
                    FullName = existingByEmail.FullName,
                    Email = existingByEmail.Email,
                    Phone = existingByEmail.Phone
                });
            }

            return Result<ClientDto>.Fail("El correo electrónico ya está registrado para un cliente.");
        }

        var firstName = !string.IsNullOrWhiteSpace(request.FirstName)
            ? request.FirstName.Trim()
            : (!string.IsNullOrWhiteSpace(request.FullName) ? request.FullName.Trim().Split(' ', 2)[0] : "Cliente");

        var lastName = !string.IsNullOrWhiteSpace(request.LastName)
            ? request.LastName.Trim()
            : (!string.IsNullOrWhiteSpace(request.FullName) && request.FullName.Trim().Contains(' ')
                ? request.FullName.Trim().Split(' ', 2)[1]
                : string.Empty);

        var client = new Client
        {
            Id = request.Id, // Usamos el ID de Supabase para coincidir
            SupabaseId = request.Id,
            FirstName = firstName,
            LastName = lastName,
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = request.Phone?.Trim()
        };

        await _clientRepository.AddAsync(client, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ClientDto>.Ok(new ClientDto
        {
            Id = client.Id,
            SupabaseId = client.SupabaseId,
            FirstName = client.FirstName,
            LastName = client.LastName,
            FullName = client.FullName,
            Email = client.Email,
            Phone = client.Phone
        });
    }
}
