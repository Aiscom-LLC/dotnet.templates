﻿using FluentValidation.Results;
using WebApiTemplate.Domain.Errors;

namespace WebApiTemplate.Application.Extensions
{
    public static class FluentValidationErrorExtension
    {
        public static ErrorResponse ToErrorResponse(this ValidationResult validationResult)
        {
            var error = validationResult.Errors.First();
            return new ErrorResponse(error.ErrorMessage);
        }
    }
}
