namespace LocalGPT.BusinessObjects;

/// <summary>Stores one user-owned DXFunction that delegates to a persisted Remote Control action pipeline.</summary>
public sealed class UserDxAiFunctionDefinition
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this user DevExpress AI function definition instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="UserDxAiFunctionDefinition"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Gets or sets the stable runtime function name. User functions use the user.* namespace.</summary>
    /// <value>The function name value exposed by <see cref="UserDxAiFunctionDefinition"/>.</value>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>Gets or sets the display name shown in the DXFunction catalog.</summary>
    /// <value>The display name value exposed by <see cref="UserDxAiFunctionDefinition"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the purpose exposed to AI and local users.</summary>
    /// <value>The purpose value exposed by <see cref="UserDxAiFunctionDefinition"/>.</value>
    public string Purpose { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the safety notes value that forms part of the user DevExpress AI function definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The safety notes value exposed by <see cref="UserDxAiFunctionDefinition"/>.</value>
    public string SafetyNotes { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the parameter schema JSON value that forms part of the user DevExpress AI function definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The parameter schema JSON value exposed by <see cref="UserDxAiFunctionDefinition"/>.</value>
    public string ParameterSchemaJson { get; set; } = "{\"type\":\"object\",\"properties\":{},\"additionalProperties\":true}";
    /// <summary>Gets or sets the Remote Control pipeline key that implements this function.</summary>
    /// <value>The pipeline key value exposed by <see cref="UserDxAiFunctionDefinition"/>.</value>
    public string PipelineKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the user DevExpress AI function definition state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="UserDxAiFunctionDefinition"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>Gets or sets whether the function may be revealed to AI chat.</summary>
    /// <value>The available to AI value exposed by <see cref="UserDxAiFunctionDefinition"/>.</value>
    public bool AvailableToAi { get; set; } = true;
    /// <summary>Gets or sets whether the composed pipeline is declared read-only.</summary>
    /// <value>The is read only value exposed by <see cref="UserDxAiFunctionDefinition"/>.</value>
    public bool IsReadOnly { get; set; } = true;
    /// <summary>Gets or sets whether invocation requires the existing Human Collaboration approval gate.</summary>
    /// <value>The requires human confirmation value exposed by <see cref="UserDxAiFunctionDefinition"/>.</value>
    public bool RequiresHumanConfirmation { get; set; }
    /// <summary>Gets or sets whether read-only/coordination-safe automatic invocation is permitted.</summary>
    /// <value>The supports automatic invocation value exposed by <see cref="UserDxAiFunctionDefinition"/>.</value>
    public bool SupportsAutomaticInvocation { get; set; }
    /// <summary>
    /// Gets or sets the created at UTC associated with this user DevExpress AI function definition state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="UserDxAiFunctionDefinition"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this user DevExpress AI function definition state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="UserDxAiFunctionDefinition"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Carries an explicitly confirmed create/update request for a user-owned DXFunction.</summary>
public sealed class SaveUserDxAiFunctionRequest
{
    /// <summary>Gets or sets the database identifier when updating an existing row.</summary>
    /// <value>The identifier value exposed by <see cref="SaveUserDxAiFunctionRequest"/>.</value>
    public Guid? Id { get; set; }
    /// <summary>
    /// Gets or sets the function name value that forms part of the save user DevExpress AI function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The function name value exposed by <see cref="SaveUserDxAiFunctionRequest"/>.</value>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the save user DevExpress AI function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="SaveUserDxAiFunctionRequest"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the purpose value that forms part of the save user DevExpress AI function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The purpose value exposed by <see cref="SaveUserDxAiFunctionRequest"/>.</value>
    public string Purpose { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the safety notes value that forms part of the save user DevExpress AI function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The safety notes value exposed by <see cref="SaveUserDxAiFunctionRequest"/>.</value>
    public string SafetyNotes { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the parameter schema JSON value that forms part of the save user DevExpress AI function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The parameter schema JSON value exposed by <see cref="SaveUserDxAiFunctionRequest"/>.</value>
    public string ParameterSchemaJson { get; set; } = "{\"type\":\"object\",\"properties\":{},\"additionalProperties\":true}";
    /// <summary>
    /// Gets or sets the stable pipeline key used to identify or correlate this save user DevExpress AI function instance with related application state.
    /// </summary>
    /// <value>The pipeline key value exposed by <see cref="SaveUserDxAiFunctionRequest"/>.</value>
    public string PipelineKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the save user DevExpress AI function state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="SaveUserDxAiFunctionRequest"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether available to AI applies to the save user DevExpress AI function state.
    /// </summary>
    /// <value>The available to AI value exposed by <see cref="SaveUserDxAiFunctionRequest"/>.</value>
    public bool AvailableToAi { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether read only applies to the save user DevExpress AI function state.
    /// </summary>
    /// <value>The is read only value exposed by <see cref="SaveUserDxAiFunctionRequest"/>.</value>
    public bool IsReadOnly { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether requires human confirmation applies to the save user DevExpress AI function state.
    /// </summary>
    /// <value>The requires human confirmation value exposed by <see cref="SaveUserDxAiFunctionRequest"/>.</value>
    public bool RequiresHumanConfirmation { get; set; }
    /// <summary>Gets or sets whether automatic invocation is allowed when the registry's safety rules also permit it.</summary>
    /// <value>The supports automatic invocation value exposed by <see cref="SaveUserDxAiFunctionRequest"/>.</value>
    public bool SupportsAutomaticInvocation { get; set; }
    /// <summary>Gets or sets whether the local user explicitly confirmed this definition mutation.</summary>
    /// <value>The user confirmed value exposed by <see cref="SaveUserDxAiFunctionRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>Carries a confirmed delete request for one user-owned DXFunction.</summary>
public sealed class DeleteUserDxAiFunctionRequest
{
    /// <summary>
    /// Gets or sets the function name value that forms part of the delete user DevExpress AI function state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The function name value exposed by <see cref="DeleteUserDxAiFunctionRequest"/>.</value>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the delete user DevExpress AI function state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="DeleteUserDxAiFunctionRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}
