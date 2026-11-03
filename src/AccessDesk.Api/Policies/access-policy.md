# AccessDesk access policy (demo data)

This is fictional demo content for the AccessDesk assistant. It does not describe a real company.

## Who can approve access requests

Managers and admins can approve or reject access requests. Employees cannot approve anything. Nobody can decide their own request, even an admin: a second person must decide it. A request can only be decided once, so a rejected request needs a new request, not a reversal.

## Approval time targets

Managers should decide a standard request within two business days. Requests for production systems should be decided within one business day. A request that is still pending after five business days is escalated to the admin team.

## GitHub access

Any employee can request read access to the company GitHub organization. Write access to a repository needs approval from the repository's team manager. Organization owner rights are reserved for admins and need a written justification that names the repository or task.

## Jira and Confluence

Jira and Confluence access is granted by the employee's manager. Access is limited to the spaces and projects that the employee's team works on. Access to another team's space needs approval from that team's manager as well.

## Azure subscriptions

Access to the development Azure subscription needs manager approval. Access to the production subscription needs manager approval and admin approval, and is granted for a maximum of 30 days. Production access is read-only unless the justification names a change ticket.

## Production database access

Direct access to a production database is not given for routine work. It is only granted for an incident or a change ticket, for at most 24 hours, with both manager and admin approval. Every session is written to the audit log. Queries that change data need a second person to review them first.

## Writing a good justification

A justification must say what the access is for and which task or ticket needs it. It must be at least 10 characters long and at most 2000. Requests with vague reasons such as "need access" are rejected, and the rejection comment will ask for more detail.

## Audit log

Every request creation and every decision writes an entry to the audit log with the person, the action and the time. Only admins can read the audit log. Audit entries cannot be edited or deleted.

## Access reviews

Admins review all active access every quarter. Managers confirm which of their team's access is still needed. Access that is not confirmed within 14 days of the review request is removed.

## Leaving the company or changing teams

When an employee leaves, all access is removed on their last day by the admin team. When an employee changes teams, the old manager's approvals expire after 14 days and the employee must request access again from the new manager.
