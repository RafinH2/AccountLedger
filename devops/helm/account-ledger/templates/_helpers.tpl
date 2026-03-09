{{- define "account-ledger.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "account-ledger.fullname" -}}
{{- if .Values.fullnameOverride -}}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- include "account-ledger.name" . -}}
{{- end -}}
{{- end -}}

{{- define "account-ledger.namespace" -}}
{{- default .Release.Namespace .Values.namespace.name -}}
{{- end -}}

{{- define "account-ledger.apiName" -}}
{{- printf "%s-api" (include "account-ledger.fullname" .) | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "account-ledger.postgresName" -}}
{{- default "postgres" .Values.postgres.name | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "account-ledger.configMapName" -}}
{{- printf "%s-config" (include "account-ledger.fullname" .) | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "account-ledger.secretName" -}}
{{- default (printf "%s-secret" (include "account-ledger.fullname" .)) .Values.secret.name | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "account-ledger.labels" -}}
app.kubernetes.io/name: {{ include "account-ledger.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
helm.sh/chart: {{ printf "%s-%s" .Chart.Name .Chart.Version | quote }}
{{- end -}}

{{- define "account-ledger.connectionString" -}}
{{- if .Values.secret.connectionString -}}
{{- .Values.secret.connectionString -}}
{{- else -}}
{{- printf "Host=%s;Port=%v;Database=%s;Username=%s;Password=%s" (include "account-ledger.postgresName" .) .Values.postgres.service.port .Values.postgres.auth.database .Values.postgres.auth.username .Values.postgres.auth.password -}}
{{- end -}}
{{- end -}}
